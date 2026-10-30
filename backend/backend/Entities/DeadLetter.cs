namespace backend.Entities;

public class DeadLetter
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DestinationId { get; set; }
    public string Url { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public Guid EventTypeId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = [];
    public int AttemptCount { get; set; }
    public int? LastStatusCode { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTime DeadLetteredAt { get; set; } = DateTime.UtcNow;
}
