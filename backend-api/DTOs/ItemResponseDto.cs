namespace backend_api.DTOs;

public class ItemResponseDto
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
