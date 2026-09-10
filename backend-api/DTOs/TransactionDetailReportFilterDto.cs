using backend_api.Models.Enums;

namespace backend_api.DTOs;

public class TransactionDetailReportFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? StoreId { get; set; }
    public int? ItemId { get; set; }
    public TransactionType? TransactionType { get; set; }
}
