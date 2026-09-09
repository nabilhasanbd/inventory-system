using backend_api.Common;
using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models.Entities;
using backend_api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Services;

public class StockTransactionService : IStockTransactionService
{
    private readonly ApplicationDbContext _db;
    private readonly IStockBalanceService _balanceService;

    public StockTransactionService(ApplicationDbContext db, IStockBalanceService balanceService)
    {
        _db = db;
        _balanceService = balanceService;
    }

    public async Task<IEnumerable<StockTransactionResponseDto>> GetListAsync(StockTransactionFilterDto filter, CancellationToken ct)
    {
        var query = _db.StockTransactions
            .Include(t => t.Store)
            .Include(t => t.Details).ThenInclude(d => d.Item)
            .AsNoTracking();

        if (filter.FromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(filter.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(t => t.TransactionDate >= from);
        }
        if (filter.ToDate.HasValue)
        {
            var to = DateTime.SpecifyKind(filter.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(t => t.TransactionDate <= to);
        }
        if (filter.TransactionType.HasValue)
            query = query.Where(t => t.TransactionType == filter.TransactionType.Value);
        if (filter.StoreId.HasValue)
            query = query.Where(t => t.StoreId == filter.StoreId.Value);
        if (!string.IsNullOrWhiteSpace(filter.TransactionNo))
            query = query.Where(t => t.TransactionNo.Contains(filter.TransactionNo));

        var transactions = await query.OrderByDescending(t => t.TransactionDate).ToListAsync(ct);
        return transactions.Select(Map).ToList();
    }

    public async Task<StockTransactionResponseDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var transaction = await _db.StockTransactions
            .Include(t => t.Store)
            .Include(t => t.Details).ThenInclude(d => d.Item)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException($"Transaction with id {id} was not found.");
        return Map(transaction);
    }

    public async Task<StockTransactionResponseDto> CreateReceiveAsync(CreateStockTransactionDto dto, CancellationToken ct)
    {
        var store = await ValidateHeaderAsync(dto, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var transaction = BuildTransaction(dto, store, TransactionType.Receipt);
        _db.StockTransactions.Add(transaction);
        await _db.SaveChangesAsync(ct);

        foreach (var d in dto.Details)
            await _balanceService.IncreaseStockAsync(store.Id, d.ItemId, d.Quantity, ct);

        await tx.CommitAsync(ct);
        return await GetByIdAsync(transaction.Id, ct);
    }

    public async Task<StockTransactionResponseDto> CreateIssueAsync(CreateStockTransactionDto dto, CancellationToken ct)
    {
        var store = await ValidateHeaderAsync(dto, ct);

        var requested = dto.Details
            .GroupBy(d => d.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

        foreach (var (itemId, qty) in requested)
        {
            var available = await _balanceService.GetCurrentStockAsync(store.Id, itemId, ct);
            if (available < qty)
                throw new ConflictException($"Insufficient stock for item {itemId}. Available: {available}, requested: {qty}.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var transaction = BuildTransaction(dto, store, TransactionType.Issue);
        _db.StockTransactions.Add(transaction);
        await _db.SaveChangesAsync(ct);

        foreach (var d in dto.Details)
            await _balanceService.DecreaseStockAsync(store.Id, d.ItemId, d.Quantity, ct);

        await tx.CommitAsync(ct);
        return await GetByIdAsync(transaction.Id, ct);
    }

    public async Task<StockTransactionResponseDto> UpdateAsync(int id, UpdateStockTransactionDto dto, CancellationToken ct)
    {
        var transaction = await _db.StockTransactions
            .Include(t => t.Details)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException($"Transaction with id {id} was not found.");

        var direction = transaction.TransactionType == TransactionType.Receipt ? 1 : -1;
        var storeId = transaction.StoreId;

        var oldQty = transaction.Details
            .GroupBy(d => d.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

        var existingById = transaction.Details.ToDictionary(d => d.Id);
        var itemIds = new HashSet<int>();
        foreach (var d in dto.Details)
        {
            if (d.Id > 0)
            {
                if (!existingById.TryGetValue(d.Id, out var existing))
                    throw new ConflictException($"Detail with id {d.Id} does not belong to transaction {id}.");
                if (d.ItemId > 0 && d.ItemId != existing.ItemId)
                    throw new ConflictException($"Cannot change item of detail {d.Id}.");
                itemIds.Add(existing.ItemId);
            }
            else
            {
                if (d.ItemId <= 0)
                    throw new BadRequestException("ItemId is required for new details.");
                itemIds.Add(d.ItemId);
            }
        }

        var items = await _db.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        foreach (var itemId in itemIds)
        {
            if (!items.TryGetValue(itemId, out var item))
                throw new NotFoundException($"Item with id {itemId} was not found.");
            if (!item.IsActive)
                throw new ConflictException($"Item with id {itemId} is not active.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var matched = new HashSet<int>();
        foreach (var d in dto.Details)
        {
            if (d.Id > 0 && existingById.TryGetValue(d.Id, out var existing))
            {
                existing.Quantity = d.Quantity;
                existing.Unit = d.Unit;
                existing.Remarks = d.Remarks;
                matched.Add(d.Id);
            }
            else
            {
                transaction.Details.Add(new StockTransactionDetail
                {
                    ItemId = d.ItemId,
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    Remarks = d.Remarks
                });
            }
        }

        var toRemove = transaction.Details.Where(d => d.Id != 0 && !matched.Contains(d.Id)).ToList();
        foreach (var d in toRemove)
            transaction.Details.Remove(d);

        if (dto.TransactionDate.HasValue)
            transaction.TransactionDate = dto.TransactionDate.Value;
        if (dto.Remarks is not null)
            transaction.Remarks = dto.Remarks;

        await _db.SaveChangesAsync(ct);

        var newQty = transaction.Details
            .GroupBy(d => d.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

        var deltas = new Dictionary<int, decimal>();
        foreach (var itemId in oldQty.Keys.Union(newQty.Keys))
        {
            oldQty.TryGetValue(itemId, out var oldQ);
            newQty.TryGetValue(itemId, out var newQ);
            var delta = direction * (newQ - oldQ);
            if (delta != 0m)
                deltas[itemId] = delta;
        }

        await ApplyStockDeltasAsync(storeId, deltas, ct);

        await tx.CommitAsync(ct);
        return await GetByIdAsync(transaction.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var transaction = await _db.StockTransactions
            .Include(t => t.Details)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException($"Transaction with id {id} was not found.");

        var direction = transaction.TransactionType == TransactionType.Receipt ? 1 : -1;
        var storeId = transaction.StoreId;

        // Deleting reverses the stock effect (newQty becomes 0): delta = -direction * qty.
        var deltas = transaction.Details
            .GroupBy(d => d.ItemId)
            .ToDictionary(g => g.Key, g => -direction * g.Sum(d => d.Quantity));

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await ApplyStockDeltasAsync(storeId, deltas, ct);
        _db.StockTransactions.Remove(transaction);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task ApplyStockDeltasAsync(int storeId, Dictionary<int, decimal> deltas, CancellationToken ct)
    {
        foreach (var (itemId, delta) in deltas)
        {
            if (delta < 0)
            {
                var available = await _balanceService.GetCurrentStockAsync(storeId, itemId, ct);
                if (available < -delta)
                    throw new ConflictException($"Insufficient stock for item {itemId}. Available: {available}, required: {-delta}.");
            }
        }

        foreach (var (itemId, delta) in deltas)
        {
            if (delta > 0)
                await _balanceService.IncreaseStockAsync(storeId, itemId, delta, ct);
            else if (delta < 0)
                await _balanceService.DecreaseStockAsync(storeId, itemId, -delta, ct);
        }
    }

    private async Task<Store> ValidateHeaderAsync(CreateStockTransactionDto dto, CancellationToken ct)
    {
        if (await _db.StockTransactions.AnyAsync(t => t.TransactionNo == dto.TransactionNo, ct))
            throw new ConflictException($"Transaction with no '{dto.TransactionNo}' already exists.");

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == dto.StoreId, ct)
            ?? throw new NotFoundException($"Store with id {dto.StoreId} was not found.");
        if (!store.IsActive)
            throw new ConflictException($"Store with id {dto.StoreId} is not active.");

        var itemIds = dto.Details.Select(d => d.ItemId).Distinct().ToList();
        var items = await _db.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        foreach (var detail in dto.Details)
        {
            if (!items.TryGetValue(detail.ItemId, out var item))
                throw new NotFoundException($"Item with id {detail.ItemId} was not found.");
            if (!item.IsActive)
                throw new ConflictException($"Item with id {detail.ItemId} is not active.");
        }

        return store;
    }

    private static StockTransaction BuildTransaction(CreateStockTransactionDto dto, Store store, TransactionType type)
    {
        var transaction = new StockTransaction
        {
            TransactionNo = dto.TransactionNo,
            TransactionDate = dto.TransactionDate ?? DateTime.UtcNow,
            TransactionType = type,
            StoreId = store.Id,
            Remarks = dto.Remarks
        };
        foreach (var d in dto.Details)
        {
            transaction.Details.Add(new StockTransactionDetail
            {
                ItemId = d.ItemId,
                Quantity = d.Quantity,
                Unit = d.Unit,
                Remarks = d.Remarks
            });
        }
        return transaction;
    }

    private static StockTransactionResponseDto Map(StockTransaction t) => new()
    {
        Id = t.Id,
        TransactionNo = t.TransactionNo,
        TransactionDate = t.TransactionDate,
        TransactionType = t.TransactionType.ToString(),
        StoreId = t.StoreId,
        StoreCode = t.Store?.Code ?? string.Empty,
        StoreName = t.Store?.Name ?? string.Empty,
        Remarks = t.Remarks,
        Details = t.Details.Select(d => new StockTransactionDetailResponseDto
        {
            Id = d.Id,
            ItemId = d.ItemId,
            ItemCode = d.Item?.ItemCode ?? string.Empty,
            ItemName = d.Item?.ItemName ?? string.Empty,
            Quantity = d.Quantity,
            Unit = d.Unit,
            Remarks = d.Remarks
        }).ToList()
    };
}
