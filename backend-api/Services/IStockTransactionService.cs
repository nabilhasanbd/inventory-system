using backend_api.DTOs;

namespace backend_api.Services;

public interface IStockTransactionService
{
    Task<IEnumerable<StockTransactionResponseDto>> GetListAsync(StockTransactionFilterDto filter, CancellationToken ct);
    Task<StockTransactionResponseDto> GetByIdAsync(int id, CancellationToken ct);
    Task<StockTransactionResponseDto> CreateReceiveAsync(CreateStockTransactionDto dto, CancellationToken ct);
    Task<StockTransactionResponseDto> CreateIssueAsync(CreateStockTransactionDto dto, CancellationToken ct);
    Task<StockTransactionResponseDto> UpdateAsync(int id, UpdateStockTransactionDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
