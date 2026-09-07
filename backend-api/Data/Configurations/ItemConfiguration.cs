using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_api.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.ItemCode).IsRequired().HasMaxLength(50);
        builder.Property(i => i.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Category).HasMaxLength(100);
        builder.Property(i => i.Unit).IsRequired().HasMaxLength(20);
        builder.Property(i => i.ReorderLevel).HasPrecision(18, 3);

        builder.HasIndex(i => i.ItemCode).IsUnique();

        builder.HasMany(i => i.StockTransactionDetails)
            .WithOne(d => d.Item)
            .HasForeignKey(d => d.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.StockBalances)
            .WithOne(b => b.Item)
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
