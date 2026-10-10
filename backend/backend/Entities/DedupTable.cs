namespace backend.Entities;

public class DedupTable
{
    public Guid TenantId { get; set; }
    public byte[] IdempotencyHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}