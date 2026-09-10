namespace backend_api.DTOs;

public class TransactionDetailReportRowDto
{
    public int DetailId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Store { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public decimal OpeningQuantity { get; set; }
    public decimal ReceiveQuantity { get; set; }
    public decimal IssueQuantity { get; set; }
    public decimal ClosingQuantity { get; set; }
}
