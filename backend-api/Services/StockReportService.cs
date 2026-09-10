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
        var range = CreateRange(filter.FromDate, filter.ToDate);
        var history = await LoadHistoryAsync(filter.StoreId, filter.ItemId, range.ToDate, ct);

        return history
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
                var opening = range.FromDate.HasValue
                    ? group.Where(m => m.TransactionDate < range.FromDate.Value).Sum(m => ToSignedQuantity(m.TransactionType, m.Quantity))
                    : 0m;

                var receive = group
                    .Where(m => IsInPeriod(m.TransactionDate, range.FromDate, range.ToDate) && m.TransactionType == TransactionType.Receipt)
                    .Sum(m => m.Quantity);

                var issue = group
                    .Where(m => IsInPeriod(m.TransactionDate, range.FromDate, range.ToDate) && m.TransactionType == TransactionType.Issue)
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

    public async Task<IEnumerable<TransactionDetailReportRowDto>> GetTransactionDetailsAsync(TransactionDetailReportFilterDto filter, CancellationToken ct)
    {
        var range = CreateRange(filter.FromDate, filter.ToDate);
        var history = await LoadHistoryAsync(filter.StoreId, filter.ItemId, range.ToDate, ct);

        return history
            .GroupBy(line => new
            {
                line.StoreId,
                line.StoreCode,
                line.StoreName,
                line.ItemId,
                line.ItemCode,
                line.ItemName
            })
            .SelectMany(group =>
            {
                var running = 0m;
                var rows = new List<TransactionDetailReportRowDto>();

                foreach (var line in group
                    .OrderBy(x => x.TransactionDate)
                    .ThenBy(x => x.TransactionId)
                    .ThenBy(x => x.DetailId))
                {
                    var opening = running;
                    var receive = line.TransactionType == TransactionType.Receipt || line.TransactionType == TransactionType.OpeningBalance
                        ? line.Quantity
                        : 0m;
                    var issue = line.TransactionType == TransactionType.Issue
                        ? line.Quantity
                        : 0m;

                    running += ToSignedQuantity(line.TransactionType, line.Quantity);

                    if (!IsInPeriod(line.TransactionDate, range.FromDate, range.ToDate))
                        continue;
                    if (filter.TransactionType.HasValue && line.TransactionType != filter.TransactionType.Value)
                        continue;

                    rows.Add(new TransactionDetailReportRowDto
                    {
                        TransactionDate = line.TransactionDate,
                        TransactionNo = line.TransactionNo,
                        TransactionType = line.TransactionType.ToString(),
                        StoreId = group.Key.StoreId,
                        StoreCode = group.Key.StoreCode,
                        StoreName = group.Key.StoreName,
                        Store = $"{group.Key.StoreCode} - {group.Key.StoreName}",
                        ItemId = group.Key.ItemId,
                        ItemCode = group.Key.ItemCode,
                        ItemName = group.Key.ItemName,
                        Item = $"{group.Key.ItemCode} - {group.Key.ItemName}",
                        OpeningQuantity = opening,
                        ReceiveQuantity = receive,
                        IssueQuantity = issue,
                        ClosingQuantity = running
                    });
                }

                return rows;
            })
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

    private async Task<List<HistoryLine>> LoadHistoryAsync(int? storeId, int? itemId, DateTime? toDate, CancellationToken ct)
    {
        var query = _db.StockTransactionDetails
            .AsNoTracking()
            .Where(d => !storeId.HasValue || d.StockTransaction.StoreId == storeId.Value)
            .Where(d => !itemId.HasValue || d.ItemId == itemId.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.StockTransaction.TransactionDate <= toDate.Value);

        return await query
            .Select(d => new HistoryLine
            {
                TransactionId = d.StockTransactionId,
                DetailId = d.Id,
                TransactionNo = d.StockTransaction.TransactionNo,
                TransactionDate = d.StockTransaction.TransactionDate,
                TransactionType = d.StockTransaction.TransactionType,
                StoreId = d.StockTransaction.StoreId,
                StoreCode = d.StockTransaction.Store.Code,
                StoreName = d.StockTransaction.Store.Name,
                ItemId = d.ItemId,
                ItemCode = d.Item.ItemCode,
                ItemName = d.Item.ItemName,
                Unit = d.Unit,
                Quantity = d.Quantity
            })
            .ToListAsync(ct);
    }

    private static DateRange CreateRange(DateTime? fromDate, DateTime? toDate)
    {
        var range = new DateRange(NormalizeFromDate(fromDate), NormalizeToDate(toDate));

        if (range.FromDate.HasValue && range.ToDate.HasValue && range.FromDate > range.ToDate)
            throw new BadRequestException("'From Date' cannot be later than 'To Date'.");

        return range;
    }

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

    private sealed record DateRange(DateTime? FromDate, DateTime? ToDate);

    private sealed class HistoryLine
    {
        public int TransactionId { get; set; }
        public int DetailId { get; set; }
        public string TransactionNo { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public TransactionType TransactionType { get; set; }
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public int ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }
}
