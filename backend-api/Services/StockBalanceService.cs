using backend_api.Common;
using backend_api.Data;
using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Services;

public class StockBalanceService : IStockBalanceService
{
    private readonly ApplicationDbContext _db;

    public StockBalanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetCurrentStockAsync(int storeId, int itemId, CancellationToken ct)
    {
        var balance = await _db.StockBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.StoreId == storeId && b.ItemId == itemId, ct);
        return balance?.Quantity ?? 0m;
    }

    public async Task<decimal> IncreaseStockAsync(int storeId, int itemId, decimal quantity, CancellationToken ct)
    {
        if (quantity <= 0)
            throw new BadRequestException("Quantity must be greater than zero.");

        if (!await _db.Stores.AnyAsync(s => s.Id == storeId, ct))
            throw new NotFoundException($"Store with id {storeId} was not found.");
        if (!await _db.Items.AnyAsync(i => i.Id == itemId, ct))
            throw new NotFoundException($"Item with id {itemId} was not found.");

        var balance = await _db.StockBalances
            .FirstOrDefaultAsync(b => b.StoreId == storeId && b.ItemId == itemId, ct);

        if (balance is null)
        {
            balance = new StockBalance { StoreId = storeId, ItemId = itemId, Quantity = quantity };
            _db.StockBalances.Add(balance);
        }
        else
        {
            balance.Quantity += quantity;
        }

        await _db.SaveChangesAsync(ct);
        return balance.Quantity;
    }

    public async Task<decimal> DecreaseStockAsync(int storeId, int itemId, decimal quantity, CancellationToken ct)
    {
        if (quantity <= 0)
            throw new BadRequestException("Quantity must be greater than zero.");

        var balance = await _db.StockBalances
            .FirstOrDefaultAsync(b => b.StoreId == storeId && b.ItemId == itemId, ct)
            ?? throw new ConflictException($"No stock balance found for store {storeId}, item {itemId}.");

        if (balance.Quantity < quantity)
            throw new ConflictException($"Insufficient stock. Available: {balance.Quantity}, requested: {quantity}.");

        balance.Quantity -= quantity;
        await _db.SaveChangesAsync(ct);
        return balance.Quantity;
    }
}
