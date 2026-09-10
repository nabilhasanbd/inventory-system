namespace backend_api.DTOs;

public class StockMovementReportFilterDto
{
    public int? StoreId { get; set; }
    public int? ItemId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
