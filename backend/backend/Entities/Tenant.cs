namespace backend.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subscription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxConcurrentDeliveries { get; set; } = 100;
    public int RateLimitPerMinute { get; set; } = 30000;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}