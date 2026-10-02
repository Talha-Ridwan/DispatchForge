namespace backend.Entities;

public class EventType
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int BitPosition { get; set; }
    public string Name { get; set; } = string.Empty;
    
}