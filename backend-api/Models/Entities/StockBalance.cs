namespace backend_api.Models.Entities;

public class StockBalance
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }

    public Store Store { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
