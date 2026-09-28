using System.ComponentModel.DataAnnotations;
using backend.DataStructure;

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
    public TenantStatus Status { get; set; }

    [Required]
    [Range(1,1000)]
    public int MaxConcurrentDeliveries { get; set; } = 100;
    [Required] 
    [Range(1,30000)]
    public int RateLimitPerMinute {get; set;} = 15000;
}