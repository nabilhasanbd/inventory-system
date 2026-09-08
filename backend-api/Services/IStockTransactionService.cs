using backend_api.DTOs;

namespace backend_api.Services;

public interface IStockTransactionService
{
    Task<StockTransactionResponseDto> CreateReceiveAsync(CreateStockTransactionDto dto, CancellationToken ct);
    Task<StockTransactionResponseDto> CreateIssueAsync(CreateStockTransactionDto dto, CancellationToken ct);
}
