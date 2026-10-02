using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class EventTypeRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
