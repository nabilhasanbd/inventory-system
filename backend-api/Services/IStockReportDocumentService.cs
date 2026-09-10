using backend_api.DTOs;

namespace backend_api.Services;

public interface IStockReportDocumentService
{
    Task<ReportDocumentResultDto> RenderStockMovementAsync(
        StockMovementReportFilterDto filter,
        string? format,
        CancellationToken ct);
    Task<ReportDocumentResultDto> RenderTransactionDetailsAsync(
        TransactionDetailReportFilterDto filter,
        string? format,
        CancellationToken ct);
}
