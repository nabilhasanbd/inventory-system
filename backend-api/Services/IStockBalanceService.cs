namespace backend_api.Services;

public interface IStockBalanceService
{
    Task<decimal> GetCurrentStockAsync(int storeId, int itemId, CancellationToken ct);
    Task<decimal> IncreaseStockAsync(int storeId, int itemId, decimal quantity, CancellationToken ct);
    Task<decimal> DecreaseStockAsync(int storeId, int itemId, decimal quantity, CancellationToken ct);
}
