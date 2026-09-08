using System.ComponentModel.DataAnnotations;

namespace backend_api.DTOs;

public class UpdateStockTransactionDto
{
    public DateTime? TransactionDate { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [MinLength(1)]
    public List<UpdateStockTransactionDetailDto> Details { get; set; } = new();
}

public class UpdateStockTransactionDetailDto
{
    // Id > 0 -> existing detail to modify; Id = 0 -> new detail to insert
    public int Id { get; set; }

    // Required for new details; ignored for existing (item cannot be changed)
    public int ItemId { get; set; }

    [Range(typeof(decimal), "0.001", "999999999")]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Remarks { get; set; }
}
