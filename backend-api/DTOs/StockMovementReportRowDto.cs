namespace backend_api.DTOs;

public class StockMovementReportRowDto
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Store { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Opening { get; set; }
    public decimal Receive { get; set; }
    public decimal Issue { get; set; }
    public decimal Return { get; set; }
    public decimal Closing { get; set; }
}
