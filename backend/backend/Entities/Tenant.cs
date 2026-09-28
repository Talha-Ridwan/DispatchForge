using backend.DataStructure;

namespace backend.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subscription { get; set; } = string.Empty;
    public TenantStatus TenantStatus { get; set; }
    public int MaxConcurrentDeliveries { get; set; } = 100;
    public int RateLimitPerMinute { get; set; } = 30000;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}