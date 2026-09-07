using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_api.Data.Configurations;

public class StockTransactionDetailConfiguration : IEntityTypeConfiguration<StockTransactionDetail>
{
    public void Configure(EntityTypeBuilder<StockTransactionDetail> builder)
    {
        builder.ToTable("StockTransactionDetails");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.Quantity).HasPrecision(18, 3);
        builder.Property(d => d.Unit).IsRequired().HasMaxLength(20);
        builder.Property(d => d.Remarks).HasMaxLength(500);
    }
}
