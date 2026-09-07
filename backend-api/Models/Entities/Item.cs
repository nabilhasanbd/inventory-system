namespace backend_api.Models.Entities;

public class Item
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StockTransactionDetail> StockTransactionDetails { get; set; } = new List<StockTransactionDetail>();
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}
