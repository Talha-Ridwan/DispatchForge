using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class UserRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;
    [Required]
    [StringLength(1000, MinimumLength = 8)]
    public string Password { get; set; }  = string.Empty;
    [Required]
    [EmailAddress]
    [StringLength(50), MinLength(2)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [RegularExpression("Admin", ErrorMessage = "Non Admin users not allowed for now")]
    public string Role { get; set; } =  string.Empty;
}