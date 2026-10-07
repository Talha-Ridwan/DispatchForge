namespace backend.DTOs;

public class EventResponseDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}
