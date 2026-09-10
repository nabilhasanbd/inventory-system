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
