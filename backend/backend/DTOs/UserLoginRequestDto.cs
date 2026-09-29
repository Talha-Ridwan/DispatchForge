using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class UserLoginRequestDto
{   
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}