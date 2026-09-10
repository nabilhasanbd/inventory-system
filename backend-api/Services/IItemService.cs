using backend_api.DTOs;

namespace backend_api.Services;

public interface IItemService
{
    Task<IEnumerable<ItemResponseDto>> GetAllAsync(CancellationToken ct);
    Task<ItemResponseDto> GetByIdAsync(int id, CancellationToken ct);
    Task<ItemResponseDto> CreateAsync(CreateItemDto dto, CancellationToken ct);
    Task<ItemResponseDto> UpdateAsync(int id, UpdateItemDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task<ItemResponseDto> SetActiveStatusAsync(int id, bool isActive, CancellationToken ct);
}
