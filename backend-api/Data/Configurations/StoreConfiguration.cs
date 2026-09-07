using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_api.Data.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(s => s.Code).IsUnique();

        builder.HasMany(s => s.StockTransactions)
            .WithOne(t => t.Store)
            .HasForeignKey(t => t.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.StockBalances)
            .WithOne(b => b.Store)
            .HasForeignKey(b => b.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
