

using backend.DataStructure;

namespace backend.Entities;

public class Destination
{
    public Guid Id { get; set; }
    //bitmask, deliberate 1NF tradeoff for performance
    public long EventFilter { get; set; }
    public string CryptographyKeyPrimary { get; set; } = string.Empty;
    public string? CryptographyKeySecondary { get; set; }
    public CircuitState CircuitState { get; set; }
    public string Url { get; set; } = string.Empty;
    public int TimeoutMilliseconds { get; set; } = 20000;
    public Guid TenantId { get; set; }
}