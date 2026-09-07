using backend_api.Models.Enums;

namespace backend_api.Models.Entities;

public class StockTransaction
{
    public int Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public int StoreId { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Store Store { get; set; } = null!;
    public ICollection<StockTransactionDetail> Details { get; set; } = new List<StockTransactionDetail>();
}
