using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class DestinationRequestDto
{
    [Required]
    [Url]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    [Range(1000, 30000)]
    public int TimeoutMilliseconds { get; set; } = 20000;

    // Event type names; the service turns these into the EventFilter bitmask
    [Required]
    public List<string> EventTypes { get; set; } = new List<string>();
}
