using backend_api.Common;
using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Services;

public class ItemService : IItemService
{
    private readonly ApplicationDbContext _db;

    public ItemService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ItemResponseDto>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Items
            .AsNoTracking()
            .OrderBy(i => i.ItemCode)
            .Select(i => new ItemResponseDto
            {
                Id = i.Id,
                ItemCode = i.ItemCode,
                ItemName = i.ItemName,
                Category = i.Category,
                Unit = i.Unit,
                ReorderLevel = i.ReorderLevel,
                IsActive = i.IsActive,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<ItemResponseDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Item with id {id} was not found.");
        return Map(item);
    }

    public async Task<ItemResponseDto> CreateAsync(CreateItemDto dto, CancellationToken ct)
    {
        if (await _db.Items.AnyAsync(i => i.ItemCode == dto.ItemCode, ct))
            throw new ConflictException($"Item with code '{dto.ItemCode}' already exists.");

        var item = new Item
        {
            ItemCode = dto.ItemCode,
            ItemName = dto.ItemName,
            Category = dto.Category,
            Unit = dto.Unit,
            ReorderLevel = dto.ReorderLevel,
            IsActive = dto.IsActive
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<ItemResponseDto> UpdateAsync(int id, UpdateItemDto dto, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Item with id {id} was not found.");

        if (await _db.Items.AnyAsync(i => i.ItemCode == dto.ItemCode && i.Id != id, ct))
            throw new ConflictException($"Item with code '{dto.ItemCode}' already exists.");

        item.ItemCode = dto.ItemCode;
        item.ItemName = dto.ItemName;
        item.Category = dto.Category;
        item.Unit = dto.Unit;
        item.ReorderLevel = dto.ReorderLevel;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<ItemResponseDto> SetActiveStatusAsync(int id, bool isActive, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Item with id {id} was not found.");

        item.IsActive = isActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(item);
    }

    private static ItemResponseDto Map(Item i) => new()
    {
        Id = i.Id,
        ItemCode = i.ItemCode,
        ItemName = i.ItemName,
        Category = i.Category,
        Unit = i.Unit,
        ReorderLevel = i.ReorderLevel,
        IsActive = i.IsActive,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt
    };
}
