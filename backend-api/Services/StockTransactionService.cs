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
        return Map(transaction);
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
        return Map(transaction);
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
        Remarks = t.Remarks,
        Details = t.Details.Select(d => new StockTransactionDetailResponseDto
        {
            Id = d.Id,
            ItemId = d.ItemId,
            Quantity = d.Quantity,
            Unit = d.Unit,
            Remarks = d.Remarks
        }).ToList()
    };
}
