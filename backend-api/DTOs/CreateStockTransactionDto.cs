using System.ComponentModel.DataAnnotations;

namespace backend_api.DTOs;

public class CreateStockTransactionDto
{
    [Required]
    [MaxLength(50)]
    public string TransactionNo { get; set; } = string.Empty;

    public DateTime? TransactionDate { get; set; }

    [Range(1, int.MaxValue)]
    public int StoreId { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [MinLength(1)]
    public List<CreateStockTransactionDetailDto> Details { get; set; } = new();
}

public class CreateStockTransactionDetailDto
{
    [Range(1, int.MaxValue)]
    public int ItemId { get; set; }

    [Range(typeof(decimal), "0.001", "999999999")]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Remarks { get; set; }
}
