namespace backend.DTOs;

// Returned only by create: the signing key is shown once and never again
public class DestinationCreatedResponseDto : DestinationResponseDto
{
    public string SigningKey { get; set; } = string.Empty;
}
