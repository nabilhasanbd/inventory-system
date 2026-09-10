using backend_api.Common;
using backend_api.Data;
using backend_api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.NETCore;

namespace backend_api.Services;

public class StockReportDocumentService : IStockReportDocumentService
{
    private const string StockMovementReportPath = "Reports/StockMovementReport.rdlc";
    private const string TransactionDetailReportPath = "Reports/TransactionDetailReport.rdlc";
    private readonly IStockReportService _stockReportService;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public StockReportDocumentService(
        IStockReportService stockReportService,
        ApplicationDbContext db,
        IWebHostEnvironment environment)
    {
        _stockReportService = stockReportService;
        _db = db;
        _environment = environment;
    }

    public async Task<ReportDocumentResultDto> RenderStockMovementAsync(
        StockMovementReportFilterDto filter,
        string? format,
        CancellationToken ct)
    {
        var normalizedFormat = NormalizeFormat(format);
        var rows = (await _stockReportService.GetStockMovementAsync(filter, ct)).ToList();
        var parameters = await BuildParametersAsync(
            "Stock Movement Report",
            filter.FromDate,
            filter.ToDate,
            filter.StoreId,
            filter.ItemId,
            ct);

        return RenderReport(
            StockMovementReportPath,
            "StockMovementDataSet",
            rows,
            parameters,
            normalizedFormat,
            "stock-movement");
    }

    public async Task<ReportDocumentResultDto> RenderTransactionDetailsAsync(
        TransactionDetailReportFilterDto filter,
        string? format,
        CancellationToken ct)
    {
        var normalizedFormat = NormalizeFormat(format);
        var rows = (await _stockReportService.GetTransactionDetailsAsync(filter, ct)).ToList();
        var parameters = await BuildParametersAsync(
            "Transaction Detail Report",
            filter.FromDate,
            filter.ToDate,
            filter.StoreId,
            filter.ItemId,
            ct);

        return RenderReport(
            TransactionDetailReportPath,
            "TransactionDetailDataSet",
            rows,
            parameters,
            normalizedFormat,
            "transaction-detail");
    }

    private ReportDocumentResultDto RenderReport<T>(
        string relativePath,
        string dataSetName,
        IEnumerable<T> rows,
        ReportParameter[] parameters,
        string format,
        string filePrefix)
    {
        var reportFile = Path.Combine(_environment.ContentRootPath, relativePath);

        if (!File.Exists(reportFile))
            throw new NotFoundException("Report definition was not found.");

        using var stream = File.OpenRead(reportFile);
        using var report = new LocalReport();
        report.LoadReportDefinition(stream);
        report.DataSources.Add(new ReportDataSource(dataSetName, rows));
        report.SetParameters(parameters);

        var content = report.Render(format);

        return new ReportDocumentResultDto
        {
            Content = content,
            ContentType = GetContentType(format),
            FileName = $"{filePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.{GetExtension(format)}"
        };
    }

    private async Task<ReportParameter[]> BuildParametersAsync(
        string title,
        DateTime? fromDate,
        DateTime? toDate,
        int? storeId,
        int? itemId,
        CancellationToken ct)
    {
        var storeText = "All Stores";
        if (storeId.HasValue)
        {
            var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == storeId.Value, ct);
            storeText = store is null ? $"Store #{storeId.Value}" : $"{store.Code} - {store.Name}";
        }

        var itemText = "All Items";
        if (itemId.HasValue)
        {
            var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == itemId.Value, ct);
            itemText = item is null ? $"Item #{itemId.Value}" : $"{item.ItemCode} - {item.ItemName}";
        }

        return
        [
            new ReportParameter("ReportTitle", title),
            new ReportParameter("DateRange", BuildDateRangeText(fromDate, toDate)),
            new ReportParameter("StoreFilter", storeText),
            new ReportParameter("ItemFilter", itemText)
        ];
    }

    private static string BuildDateRangeText(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue)
            return $"{fromDate.Value:dd-MMM-yyyy} to {toDate.Value:dd-MMM-yyyy}";
        if (fromDate.HasValue)
            return $"From {fromDate.Value:dd-MMM-yyyy}";
        if (toDate.HasValue)
            return $"Up to {toDate.Value:dd-MMM-yyyy}";
        return "All Dates";
    }

    private static string NormalizeFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return "PDF";

        return format.Trim().ToUpperInvariant() switch
        {
            "PDF" => "PDF",
            "XLSX" => "EXCELOPENXML",
            "EXCEL" => "EXCELOPENXML",
            "EXCELOPENXML" => "EXCELOPENXML",
            _ => throw new BadRequestException("Unsupported report format. Use PDF or XLSX.")
        };
    }

    private static string GetContentType(string format) => format switch
    {
        "PDF" => "application/pdf",
        "EXCELOPENXML" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };

    private static string GetExtension(string format) => format switch
    {
        "PDF" => "pdf",
        "EXCELOPENXML" => "xlsx",
        _ => "bin"
    };
}
