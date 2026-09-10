using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models.Entities;
using backend_api.Models.Enums;
using backend_api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace backend_api.Tests;

public class StockReportServiceTests
{
    [Fact]
    public async Task GetStockMovementAsync_CalculatesMovementFromTransactionHistory()
    {
        await using var db = await CreateReportDbAsync();
        var service = new StockReportService(db);

        var report = (await service.GetStockMovementAsync(new StockMovementReportFilterDto
        {
            StoreId = 1,
            ItemId = 1,
            FromDate = new DateTime(2026, 9, 5),
            ToDate = new DateTime(2026, 9, 6)
        }, CancellationToken.None)).Single();

        Assert.Equal(1, report.ItemId);
        Assert.Equal(1, report.StoreId);
        Assert.Equal(90m, report.Opening);
        Assert.Equal(20m, report.Receive);
        Assert.Equal(15m, report.Issue);
        Assert.Equal(0m, report.Return);
        Assert.Equal(95m, report.Closing);
    }

    [Fact]
    public async Task GetTransactionDetailsAsync_CalculatesRunningStockAcrossMultipleTransactions()
    {
        await using var db = await CreateReportDbAsync();
        var service = new StockReportService(db);

        var report = (await service.GetTransactionDetailsAsync(new TransactionDetailReportFilterDto
        {
            StoreId = 1,
            ItemId = 1,
            FromDate = new DateTime(2026, 9, 3),
            ToDate = new DateTime(2026, 9, 6)
        }, CancellationToken.None)).ToList();

        Assert.Collection(report,
            row =>
            {
                Assert.Equal(new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc), row.TransactionDate);
                Assert.Equal("TXN-002", row.TransactionNo);
                Assert.Equal("Receipt", row.TransactionType);
                Assert.Equal(50m, row.OpeningQuantity);
                Assert.Equal(50m, row.ReceiveQuantity);
                Assert.Equal(0m, row.IssueQuantity);
                Assert.Equal(100m, row.ClosingQuantity);
            },
            row =>
            {
                Assert.Equal(new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc), row.TransactionDate);
                Assert.Equal("TXN-003", row.TransactionNo);
                Assert.Equal("Issue", row.TransactionType);
                Assert.Equal(100m, row.OpeningQuantity);
                Assert.Equal(0m, row.ReceiveQuantity);
                Assert.Equal(10m, row.IssueQuantity);
                Assert.Equal(90m, row.ClosingQuantity);
            },
            row =>
            {
                Assert.Equal(new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc), row.TransactionDate);
                Assert.Equal("TXN-004", row.TransactionNo);
                Assert.Equal("Receipt", row.TransactionType);
                Assert.Equal(90m, row.OpeningQuantity);
                Assert.Equal(20m, row.ReceiveQuantity);
                Assert.Equal(0m, row.IssueQuantity);
                Assert.Equal(110m, row.ClosingQuantity);
            },
            row =>
            {
                Assert.Equal(new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc), row.TransactionDate);
                Assert.Equal("TXN-005", row.TransactionNo);
                Assert.Equal("Issue", row.TransactionType);
                Assert.Equal(110m, row.OpeningQuantity);
                Assert.Equal(0m, row.ReceiveQuantity);
                Assert.Equal(15m, row.IssueQuantity);
                Assert.Equal(95m, row.ClosingQuantity);
            });
    }

    [Fact]
    public async Task RenderStockMovementAsync_CreatesPdfFromCalculatedRows()
    {
        await using var db = await CreateReportDbAsync();
        var reportService = new StockReportService(db);
        var documentService = new StockReportDocumentService(
            reportService,
            db,
            CreateEnvironment());

        var result = await documentService.RenderStockMovementAsync(new StockMovementReportFilterDto
        {
            StoreId = 1,
            ItemId = 1,
            FromDate = new DateTime(2026, 9, 5),
            ToDate = new DateTime(2026, 9, 6)
        }, "PDF", CancellationToken.None);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.EndsWith(".pdf", result.FileName);
        Assert.NotEmpty(result.Content);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(result.Content.Take(4).ToArray()));
    }

    [Fact]
    public async Task RenderTransactionDetailsAsync_CreatesPdfFromTransactionHistory()
    {
        await using var db = await CreateReportDbAsync();
        var reportService = new StockReportService(db);
        var documentService = new StockReportDocumentService(
            reportService,
            db,
            CreateEnvironment());

        var result = await documentService.RenderTransactionDetailsAsync(new TransactionDetailReportFilterDto
        {
            StoreId = 1,
            ItemId = 1,
            FromDate = new DateTime(2026, 9, 3),
            ToDate = new DateTime(2026, 9, 6)
        }, "PDF", CancellationToken.None);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.EndsWith(".pdf", result.FileName);
        Assert.NotEmpty(result.Content);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(result.Content.Take(4).ToArray()));
    }

    private static async Task<ApplicationDbContext> CreateReportDbAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ApplicationDbContext(options);
        await SeedReportDataAsync(db);
        return db;
    }

    private static async Task SeedReportDataAsync(ApplicationDbContext db)
    {
        db.Stores.AddRange(
            new Store { Id = 1, Code = "STR-001", Name = "Main Warehouse", IsActive = true },
            new Store { Id = 2, Code = "STR-002", Name = "Branch Store", IsActive = true });

        db.Items.AddRange(
            new Item
            {
                Id = 1,
                ItemCode = "ITM-001",
                ItemName = "A4 Paper Ream",
                Unit = "REAM",
                ReorderLevel = 10,
                IsActive = true
            },
            new Item
            {
                Id = 2,
                ItemCode = "ITM-002",
                ItemName = "Ballpoint Pen Blue",
                Unit = "BOX",
                ReorderLevel = 5,
                IsActive = true
            });

        db.StockTransactions.AddRange(
            CreateTransaction(1, "TXN-001", new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), TransactionType.OpeningBalance, 1, [CreateDetail(1, 1, 1, 50m, "REAM")]),
            CreateTransaction(2, "TXN-002", new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc), TransactionType.Receipt, 1, [CreateDetail(2, 2, 1, 50m, "REAM")]),
            CreateTransaction(3, "TXN-003", new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc), TransactionType.Issue, 1, [CreateDetail(3, 3, 1, 10m, "REAM")]),
            CreateTransaction(4, "TXN-004", new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc), TransactionType.Receipt, 1, [CreateDetail(4, 4, 1, 20m, "REAM")]),
            CreateTransaction(5, "TXN-005", new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc), TransactionType.Issue, 1, [CreateDetail(5, 5, 1, 15m, "REAM")]),
            CreateTransaction(6, "TXN-006", new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc), TransactionType.Receipt, 2, [CreateDetail(6, 6, 2, 99m, "BOX")]));

        await db.SaveChangesAsync();
    }

    private static IWebHostEnvironment CreateEnvironment()
    {
        var root = FindBackendApiRoot();

        return new TestWebHostEnvironment
        {
            ContentRootPath = root,
            ContentRootFileProvider = new PhysicalFileProvider(root),
            WebRootPath = root,
            WebRootFileProvider = new PhysicalFileProvider(root),
            ApplicationName = "backend-api",
            EnvironmentName = "Development"
        };
    }

    private static string FindBackendApiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "backend-api");
            if (File.Exists(Path.Combine(candidate, "backend-api.csproj")))
                return candidate;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to find backend-api project root for report tests.");
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

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
