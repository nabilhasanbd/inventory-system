using System.ComponentModel.DataAnnotations;

namespace backend_api.DTOs;

public class UpdateItemDto
{
    [Required]
    [MaxLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal ReorderLevel { get; set; }
}
