using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<StockTransactionDetail> StockTransactionDetails => Set<StockTransactionDetail>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
