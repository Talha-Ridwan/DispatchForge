namespace backend.Entities;

public class IdempotencyRecord
{
    public Guid TenantId { get; set; }
    public required byte[] IdempotencyHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}