using backend_api.Common;
using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models.Entities;
using backend_api.Models.Enums;
using backend_api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace backend_api.Tests;

public class StockTransactionServiceTests
{
    [Fact]
    public async Task UpdateAsync_OpeningBalanceIncrease_AdjustsStockAsPositiveMovement()
    {
        await using var db = CreateDb();
        SeedTransactionData(db, TransactionType.OpeningBalance, 10m);
        var balanceService = new FakeStockBalanceService();
        var service = new StockTransactionService(db, balanceService);

        await service.UpdateAsync(1, new UpdateStockTransactionDto
        {
            TransactionDate = new DateTime(2026, 9, 2),
            Details =
            [
                new UpdateStockTransactionDetailDto
                {
                    Id = 1,
                    ItemId = 1,
                    Quantity = 15m,
                    Unit = "PCS"
                }
            ]
        }, CancellationToken.None);

        Assert.Contains(balanceService.Increases, call => call.storeId == 1 && call.itemId == 1 && call.quantity == 5m);
        Assert.Empty(balanceService.Decreases);
    }

    [Fact]
    public async Task DeleteAsync_OpeningBalance_ReversesStockAsDecrease()
    {
        await using var db = CreateDb();
        SeedTransactionData(db, TransactionType.OpeningBalance, 10m);
        var balanceService = new FakeStockBalanceService();
        var service = new StockTransactionService(db, balanceService);

        await service.DeleteAsync(1, CancellationToken.None);

        Assert.Contains(balanceService.Decreases, call => call.storeId == 1 && call.itemId == 1 && call.quantity == 10m);
        Assert.Empty(balanceService.Increases);
    }

    [Fact]
    public async Task DeleteAsync_UnsupportedStockType_ThrowsConflict()
    {
        await using var db = CreateDb();
        SeedTransactionData(db, TransactionType.Transfer, 10m);
        var service = new StockTransactionService(db, new FakeStockBalanceService());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(1, CancellationToken.None));

        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_AddsChangesRemovesDetailsAndClearsRemarks()
    {
        await using var db = CreateDb();
        SeedTransactionData(db, TransactionType.Receipt, 10m);
        db.StockTransactions.Single().Remarks = "Old remarks";
        db.Items.Add(new Item { Id = 2, ItemCode = "ITM-002", ItemName = "Paper", Unit = "PCS" });
        db.StockTransactionDetails.Add(new StockTransactionDetail
        {
            Id = 2, StockTransactionId = 1, ItemId = 2, Quantity = 4m, Unit = "PCS"
        });
        await db.SaveChangesAsync();
        var balance = new FakeStockBalanceService();
        var updated = await new StockTransactionService(db, balance).UpdateAsync(1, new UpdateStockTransactionDto
        {
            Remarks = null,
            Details = [
                new() { Id = 1, ItemId = 1, Quantity = 12m, Unit = "PCS" },
                new() { ItemId = 1, Quantity = 3m, Unit = "PCS" }
            ]
        }, CancellationToken.None);
        Assert.Null(updated.Remarks);
        Assert.Equal(2, updated.Details.Count);
        Assert.DoesNotContain(updated.Details, d => d.Id == 2);
        Assert.Contains(balance.Increases, x => x.itemId == 1 && x.quantity == 5m);
        Assert.Contains(balance.Decreases, x => x.itemId == 2 && x.quantity == 4m);
    }

    [Fact]
    public async Task DeleteItem_ProtectsHistoryButAllowsUnusedItems()
    {
        await using var db = CreateDb();
        SeedTransactionData(db, TransactionType.Receipt, 10m);
        db.Items.Add(new Item { Id = 2, ItemCode = "UNUSED", ItemName = "Unused", Unit = "PCS" });
        await db.SaveChangesAsync();
        var service = new ItemService(db);
        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(1, CancellationToken.None));
        await service.DeleteAsync(2, CancellationToken.None);
        Assert.True(await db.Items.AnyAsync(i => i.Id == 1));
        Assert.False(await db.Items.AnyAsync(i => i.Id == 2));
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SeedTransactionData(ApplicationDbContext db, TransactionType type, decimal quantity)
    {
        db.Stores.Add(new Store { Id = 1, Code = "STR-001", Name = "Main Warehouse", IsActive = true });
        db.Items.Add(new Item { Id = 1, ItemCode = "ITM-001", ItemName = "Mouse", Unit = "PCS", ReorderLevel = 1, IsActive = true });
        db.StockTransactions.Add(new StockTransaction
        {
            Id = 1,
            TransactionNo = "TXN-001",
            TransactionDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            TransactionType = type,
            StoreId = 1,
            Details =
            [
                new StockTransactionDetail
                {
                    Id = 1,
                    ItemId = 1,
                    Quantity = quantity,
                    Unit = "PCS"
                }
            ]
        });
        db.SaveChanges();
    }

    private sealed class FakeStockBalanceService : IStockBalanceService
    {
        public List<(int storeId, int itemId, decimal quantity)> Increases { get; } = [];
        public List<(int storeId, int itemId, decimal quantity)> Decreases { get; } = [];

        public Task<decimal> GetCurrentStockAsync(int storeId, int itemId, CancellationToken ct) => Task.FromResult(999m);

        public Task<decimal> IncreaseStockAsync(int storeId, int itemId, decimal quantity, CancellationToken ct)
        {
            Increases.Add((storeId, itemId, quantity));
            return Task.FromResult(quantity);
        }

        public Task<decimal> DecreaseStockAsync(int storeId, int itemId, decimal quantity, CancellationToken ct)
        {
            Decreases.Add((storeId, itemId, quantity));
            return Task.FromResult(quantity);
        }
    }
}
