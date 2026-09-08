namespace backend_api.DTOs;

public class StockTransactionResponseDto
{
    public int Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string? Remarks { get; set; }
    public List<StockTransactionDetailResponseDto> Details { get; set; } = new();
}

public class StockTransactionDetailResponseDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}
