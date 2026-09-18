namespace backend.DTOs;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } =  string.Empty;
    public string JwtToken { get; set; } = string.Empty;
}