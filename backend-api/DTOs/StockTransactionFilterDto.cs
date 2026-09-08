using backend_api.Models.Enums;

namespace backend_api.DTOs;

public class StockTransactionFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TransactionType? TransactionType { get; set; }
    public int? StoreId { get; set; }
    public string? TransactionNo { get; set; }
}
