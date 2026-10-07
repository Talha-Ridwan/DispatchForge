namespace backend.Entities;

public class DeliveryOutbox
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public DateTime EventReceivedAt { get; set; }
    public Guid DestinationId { get; set; }
    public Guid TenantId { get; set; }
    public int AttemptCount { get; set; }
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
    public string? LeasedBy { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
}
