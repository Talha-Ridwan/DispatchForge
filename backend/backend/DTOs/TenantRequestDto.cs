using System.ComponentModel.DataAnnotations;
namespace backend.DTOs;

public class TenantRequestDto
{
    [Required]
    [StringLength(30),  MinLength(3)]
    public string Name { get; set; } = string.Empty;
    [Required]
    [StringLength(30),  MinLength(3)]
    public string Subscription { get; set; } = string.Empty;
    [Required]
    [RegularExpression("Enabled", ErrorMessage = "Status must be enabled or disabled")]
    public string Status { get; set; } = string.Empty;
    [Required]
    [StringLength(30),  MinLength(3)]
    public string MaxConcurrentDeliveries { get; set; } = string.Empty;
    [Required] 
    public int RateLimitPerMinute {get; set;} = 30000;
    [Required]
    public DateTime CreatedAt { get; set; }
}