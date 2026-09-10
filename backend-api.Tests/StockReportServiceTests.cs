using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models.Entities;
using backend_api.Models.Enums;
using backend_api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_api.Tests;

public class StockReportServiceTests
{
    [Fact]
    public async Task GetStockMovementAsync_CalculatesMovementFromTransactionHistory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);

        var store = new Store { Id = 1, Code = "STR-001", Name = "Main Warehouse", IsActive = true };
        var otherStore = new Store { Id = 2, Code = "STR-002", Name = "Branch Store", IsActive = true };
        var item = new Item
        {
            Id = 1,
            ItemCode = "ITM-001",
            ItemName = "A4 Paper Ream",
            Unit = "REAM",
            ReorderLevel = 10,
            IsActive = true
        };
        var otherItem = new Item
        {
            Id = 2,
            ItemCode = "ITM-002",
            ItemName = "Ballpoint Pen Blue",
            Unit = "BOX",
            ReorderLevel = 5,
            IsActive = true
        };

        db.Stores.AddRange(store, otherStore);
        db.Items.AddRange(item, otherItem);
        db.StockTransactions.AddRange(
            CreateTransaction(
                id: 1,
                transactionNo: "TXN-001",
                transactionDate: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                transactionType: TransactionType.OpeningBalance,
                storeId: store.Id,
                details:
                [
                    CreateDetail(id: 1, stockTransactionId: 1, itemId: item.Id, quantity: 50m, unit: item.Unit)
                ]),
            CreateTransaction(
                id: 2,
                transactionNo: "TXN-002",
                transactionDate: new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc),
                transactionType: TransactionType.Receipt,
                storeId: store.Id,
                details:
                [
                    CreateDetail(id: 2, stockTransactionId: 2, itemId: item.Id, quantity: 50m, unit: item.Unit)
                ]),
            CreateTransaction(
                id: 3,
                transactionNo: "TXN-003",
                transactionDate: new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
                transactionType: TransactionType.Issue,
                storeId: store.Id,
                details:
                [
                    CreateDetail(id: 3, stockTransactionId: 3, itemId: item.Id, quantity: 10m, unit: item.Unit)
                ]),
            CreateTransaction(
                id: 4,
                transactionNo: "TXN-004",
                transactionDate: new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc),
                transactionType: TransactionType.Receipt,
                storeId: store.Id,
                details:
                [
                    CreateDetail(id: 4, stockTransactionId: 4, itemId: item.Id, quantity: 20m, unit: item.Unit)
                ]),
            CreateTransaction(
                id: 5,
                transactionNo: "TXN-005",
                transactionDate: new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc),
                transactionType: TransactionType.Issue,
                storeId: store.Id,
                details:
                [
                    CreateDetail(id: 5, stockTransactionId: 5, itemId: item.Id, quantity: 15m, unit: item.Unit)
                ]),
            CreateTransaction(
                id: 6,
                transactionNo: "TXN-006",
                transactionDate: new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
                transactionType: TransactionType.Receipt,
                storeId: otherStore.Id,
                details:
                [
                    CreateDetail(id: 6, stockTransactionId: 6, itemId: otherItem.Id, quantity: 99m, unit: otherItem.Unit)
                ]));

        await db.SaveChangesAsync();

        var service = new StockReportService(db);

        var report = (await service.GetStockMovementAsync(new StockMovementReportFilterDto
        {
            StoreId = store.Id,
            ItemId = item.Id,
            FromDate = new DateTime(2026, 9, 5),
            ToDate = new DateTime(2026, 9, 6)
        }, CancellationToken.None)).Single();

        Assert.Equal(item.Id, report.ItemId);
        Assert.Equal(store.Id, report.StoreId);
        Assert.Equal(90m, report.Opening);
        Assert.Equal(20m, report.Receive);
        Assert.Equal(15m, report.Issue);
        Assert.Equal(0m, report.Return);
        Assert.Equal(95m, report.Closing);
    }

    private static StockTransaction CreateTransaction(
        int id,
        string transactionNo,
        DateTime transactionDate,
        TransactionType transactionType,
        int storeId,
        IEnumerable<StockTransactionDetail> details) => new()
        {
            Id = id,
            TransactionNo = transactionNo,
            TransactionDate = transactionDate,
            TransactionType = transactionType,
            StoreId = storeId,
            Details = details.ToList()
        };

    private static StockTransactionDetail CreateDetail(
        int id,
        int stockTransactionId,
        int itemId,
        decimal quantity,
        string unit) => new()
        {
            Id = id,
            StockTransactionId = stockTransactionId,
            ItemId = itemId,
            Quantity = quantity,
            Unit = unit
        };
}
