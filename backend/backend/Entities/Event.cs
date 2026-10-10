namespace backend.Entities;

public class Event
{
    public Guid Id { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public Guid EventTypeId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = [];
}
