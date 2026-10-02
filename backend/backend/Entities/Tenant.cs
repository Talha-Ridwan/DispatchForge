using backend.DataStructure;

namespace backend.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TenantStatus TenantStatus { get; set; }
    public int MaxConcurrentDeliveries { get; set; } = 100;
    public int RateLimitPerMinute { get; set; } = 30000;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Destination> Destinations { get; set; } = new List<Destination>();
    public List<EventType> EventTypes { get; set; } = new List<EventType>();
}