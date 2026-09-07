using backend_api.Common;
using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Services;

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _db;

    public StoreService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<StoreResponseDto>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Stores
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new StoreResponseDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                IsActive = s.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<StoreResponseDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException($"Store with id {id} was not found.");
        return Map(store);
    }

    public async Task<StoreResponseDto> CreateAsync(CreateStoreDto dto, CancellationToken ct)
    {
        if (await _db.Stores.AnyAsync(s => s.Code == dto.Code, ct))
            throw new ConflictException($"Store with code '{dto.Code}' already exists.");

        var store = new Store
        {
            Code = dto.Code,
            Name = dto.Name,
            IsActive = dto.IsActive
        };
        _db.Stores.Add(store);
        await _db.SaveChangesAsync(ct);
        return Map(store);
    }

    public async Task<StoreResponseDto> UpdateAsync(int id, UpdateStoreDto dto, CancellationToken ct)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException($"Store with id {id} was not found.");

        if (await _db.Stores.AnyAsync(s => s.Code == dto.Code && s.Id != id, ct))
            throw new ConflictException($"Store with code '{dto.Code}' already exists.");

        store.Code = dto.Code;
        store.Name = dto.Name;
        await _db.SaveChangesAsync(ct);
        return Map(store);
    }

    public async Task<StoreResponseDto> SetActiveStatusAsync(int id, bool isActive, CancellationToken ct)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException($"Store with id {id} was not found.");

        store.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return Map(store);
    }

    private static StoreResponseDto Map(Store s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        Name = s.Name,
        IsActive = s.IsActive
    };
}
