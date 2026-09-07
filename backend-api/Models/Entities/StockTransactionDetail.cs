namespace backend_api.Models.Entities;

public class StockTransactionDetail
{
    public int Id { get; set; }
    public int StockTransactionId { get; set; }
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    public StockTransaction StockTransaction { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
