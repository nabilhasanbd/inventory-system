using backend_api.DTOs;

namespace backend_api.Services;

public interface IStockReportService
{
    Task<IEnumerable<StockMovementReportRowDto>> GetStockMovementAsync(StockMovementReportFilterDto filter, CancellationToken ct);
    Task<IEnumerable<TransactionDetailReportRowDto>> GetTransactionDetailsAsync(TransactionDetailReportFilterDto filter, CancellationToken ct);
}
