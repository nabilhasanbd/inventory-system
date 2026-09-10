using backend_api.Common;
using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Services;

public class StockReportService : IStockReportService
{
    private readonly ApplicationDbContext _db;

    public StockReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<StockMovementReportRowDto>> GetStockMovementAsync(StockMovementReportFilterDto filter, CancellationToken ct)
    {
        var fromDate = NormalizeFromDate(filter.FromDate);
        var toDate = NormalizeToDate(filter.ToDate);

        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            throw new BadRequestException("'From Date' cannot be later than 'To Date'.");

        var query = _db.StockTransactionDetails
            .AsNoTracking()
            .Where(d => !filter.StoreId.HasValue || d.StockTransaction.StoreId == filter.StoreId.Value)
            .Where(d => !filter.ItemId.HasValue || d.ItemId == filter.ItemId.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.StockTransaction.TransactionDate <= toDate.Value);

        var movements = await query
            .Select(d => new MovementLine
            {
                ItemId = d.ItemId,
                ItemCode = d.Item.ItemCode,
                ItemName = d.Item.ItemName,
                StoreId = d.StockTransaction.StoreId,
                StoreCode = d.StockTransaction.Store.Code,
                StoreName = d.StockTransaction.Store.Name,
                Unit = d.Unit,
                Quantity = d.Quantity,
                TransactionDate = d.StockTransaction.TransactionDate,
                TransactionType = d.StockTransaction.TransactionType
            })
            .ToListAsync(ct);

        return movements
            .GroupBy(m => new
            {
                m.ItemId,
                m.ItemCode,
                m.ItemName,
                m.StoreId,
                m.StoreCode,
                m.StoreName,
                m.Unit
            })
            .Select(group =>
            {
                var opening = fromDate.HasValue
                    ? group.Where(m => m.TransactionDate < fromDate.Value).Sum(m => ToSignedQuantity(m.TransactionType, m.Quantity))
                    : 0m;

                var receive = group
                    .Where(m => IsInPeriod(m.TransactionDate, fromDate, toDate) && m.TransactionType == TransactionType.Receipt)
                    .Sum(m => m.Quantity);

                var issue = group
                    .Where(m => IsInPeriod(m.TransactionDate, fromDate, toDate) && m.TransactionType == TransactionType.Issue)
                    .Sum(m => m.Quantity);

                const decimal returnQuantity = 0m;

                return new StockMovementReportRowDto
                {
                    ItemId = group.Key.ItemId,
                    ItemCode = group.Key.ItemCode,
                    ItemName = group.Key.ItemName,
                    Item = $"{group.Key.ItemCode} - {group.Key.ItemName}",
                    StoreId = group.Key.StoreId,
                    StoreCode = group.Key.StoreCode,
                    StoreName = group.Key.StoreName,
                    Store = $"{group.Key.StoreCode} - {group.Key.StoreName}",
                    Unit = group.Key.Unit,
                    Opening = opening,
                    Receive = receive,
                    Issue = issue,
                    Return = returnQuantity,
                    Closing = opening + receive - issue + returnQuantity
                };
            })
            .Where(r => r.Opening != 0m || r.Receive != 0m || r.Issue != 0m || r.Return != 0m || r.Closing != 0m)
            .OrderBy(r => r.ItemCode)
            .ThenBy(r => r.StoreCode)
            .ToList();
    }

    private static bool IsInPeriod(DateTime transactionDate, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && transactionDate < fromDate.Value)
            return false;
        if (toDate.HasValue && transactionDate > toDate.Value)
            return false;
        return true;
    }

    private static decimal ToSignedQuantity(TransactionType transactionType, decimal quantity) => transactionType switch
    {
        TransactionType.Receipt => quantity,
        TransactionType.OpeningBalance => quantity,
        TransactionType.Issue => -quantity,
        _ => 0m
    };

    private static DateTime? NormalizeFromDate(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        var normalized = EnsureUtc(value.Value);
        return normalized.TimeOfDay == TimeSpan.Zero ? normalized.Date : normalized;
    }

    private static DateTime? NormalizeToDate(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        var normalized = EnsureUtc(value.Value);
        return normalized.TimeOfDay == TimeSpan.Zero
            ? normalized.Date.AddDays(1).AddTicks(-1)
            : normalized;
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class MovementLine
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionType TransactionType { get; set; }
    }
}
