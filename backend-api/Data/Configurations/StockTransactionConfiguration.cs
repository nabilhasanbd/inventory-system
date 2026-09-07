using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_api.Data.Configurations;

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("StockTransactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.TransactionNo).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Remarks).HasMaxLength(500);

        builder.HasIndex(t => t.TransactionNo).IsUnique();
        builder.HasIndex(t => new { t.StoreId, t.TransactionDate });
        builder.HasIndex(t => t.TransactionDate);

        builder.HasMany(t => t.Details)
            .WithOne(d => d.StockTransaction)
            .HasForeignKey(d => d.StockTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
