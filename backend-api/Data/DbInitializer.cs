using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await SeedStoresAsync(db);
        await SeedItemsAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedStoresAsync(ApplicationDbContext db)
    {
        var stores = new[]
        {
            new Store { Code = "STR-001", Name = "Main Warehouse", IsActive = true },
            new Store { Code = "STR-002", Name = "Central Store", IsActive = true },
            new Store { Code = "STR-003", Name = "Branch Store - North", IsActive = true }
        };

        var existingCodes = await db.Stores.Select(s => s.Code).ToListAsync();
        var toAdd = stores.Where(s => !existingCodes.Contains(s.Code)).ToArray();
        if (toAdd.Length > 0)
            await db.Stores.AddRangeAsync(toAdd);
    }

    private static async Task SeedItemsAsync(ApplicationDbContext db)
    {
        var items = new[]
        {
            new Item { ItemCode = "ITM-001", ItemName = "A4 Paper Ream", Category = "Stationery", Unit = "REAM", ReorderLevel = 20, IsActive = true },
            new Item { ItemCode = "ITM-002", ItemName = "Ballpoint Pen Blue", Category = "Stationery", Unit = "BOX", ReorderLevel = 15, IsActive = true },
            new Item { ItemCode = "ITM-003", ItemName = "USB Cable Type-C", Category = "Electronics", Unit = "PCS", ReorderLevel = 30, IsActive = true },
            new Item { ItemCode = "ITM-004", ItemName = "Wireless Mouse", Category = "Electronics", Unit = "PCS", ReorderLevel = 10, IsActive = true },
            new Item { ItemCode = "ITM-005", ItemName = "AA Batteries (4-pack)", Category = "Electronics", Unit = "PACK", ReorderLevel = 25, IsActive = true },
            new Item { ItemCode = "ITM-006", ItemName = "Basmati Rice", Category = "Groceries", Unit = "KG", ReorderLevel = 50, IsActive = true },
            new Item { ItemCode = "ITM-007", ItemName = "Cooking Oil 5L", Category = "Groceries", Unit = "BOTTLE", ReorderLevel = 12, IsActive = true },
            new Item { ItemCode = "ITM-008", ItemName = "Mineral Water 1L", Category = "Beverages", Unit = "CARTON", ReorderLevel = 18, IsActive = true },
            new Item { ItemCode = "ITM-009", ItemName = "Tea Bags 100s", Category = "Beverages", Unit = "BOX", ReorderLevel = 8, IsActive = true },
            new Item { ItemCode = "ITM-010", ItemName = "Dishwash Liquid", Category = "Cleaning", Unit = "BOTTLE", ReorderLevel = 14, IsActive = true },
            new Item { ItemCode = "ITM-011", ItemName = "Floor Cleaner 2L", Category = "Cleaning", Unit = "BOTTLE", ReorderLevel = 9, IsActive = true },
            new Item { ItemCode = "ITM-012", ItemName = "Screwdriver Set", Category = "Hardware", Unit = "SET", ReorderLevel = 6, IsActive = true }
        };

        var existingCodes = await db.Items.Select(i => i.ItemCode).ToListAsync();
        var toAdd = items.Where(i => !existingCodes.Contains(i.ItemCode)).ToArray();
        if (toAdd.Length > 0)
            await db.Items.AddRangeAsync(toAdd);
    }
}
