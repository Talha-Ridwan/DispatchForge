using backend.DataStructure;

namespace backend.DTOs;

public class TenantResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subscription { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public int MaxConcurrentDeliveries { get; set; }
    public int RateLimitPerMinute { get; set; }
}