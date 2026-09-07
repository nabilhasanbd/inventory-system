using backend_api.DTOs;

namespace backend_api.Services;

public interface IStoreService
{
    Task<IEnumerable<StoreResponseDto>> GetAllAsync(CancellationToken ct);
    Task<StoreResponseDto> GetByIdAsync(int id, CancellationToken ct);
    Task<StoreResponseDto> CreateAsync(CreateStoreDto dto, CancellationToken ct);
    Task<StoreResponseDto> UpdateAsync(int id, UpdateStoreDto dto, CancellationToken ct);
    Task<StoreResponseDto> SetActiveStatusAsync(int id, bool isActive, CancellationToken ct);
}
