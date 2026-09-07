using System.ComponentModel.DataAnnotations;

namespace backend_api.DTOs;

public class UpdateStoreDto
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
