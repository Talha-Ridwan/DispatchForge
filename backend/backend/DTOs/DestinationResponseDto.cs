using backend.DataStructure;

namespace backend.DTOs;

public class DestinationResponseDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int TimeoutMilliseconds { get; set; }
    public CircuitState CircuitState { get; set; }
    public List<string> EventTypes { get; set; } = new List<string>();
}
