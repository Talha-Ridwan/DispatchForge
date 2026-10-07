using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace backend.DTOs;

public class EventRequestDto
{
    // Event type name; the service looks up its EventTypeId and bit
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    // Sender's "I already sent this one" key; resends with the same key are dropped
    [Required]
    [StringLength(255)]
    public string IdempotencyKey { get; set; } = string.Empty;

    // Whatever JSON the sender wants delivered; kept as-is, never mapped to a class
    [Required]
    public JsonElement Payload { get; set; }
}
