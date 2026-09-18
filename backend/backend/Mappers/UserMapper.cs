using backend.DTOs;
using backend.Entities;

namespace backend.Mappers;

public class UserMapper
{
    public UserResponseDto ToResponse(User user)
    {
        return new UserResponseDto
        {
            Email = user.Email,
            Id = user.Id,
            Role = user.Role,
            Username = user.Username
        };
    }

    public User ToEntity(UserRequestDto dto)
    {
        return new User
        {
            Username = dto.Username,
            Password = dto.Password,
            Email = dto.Email,
            Role = dto.Role
        };
    }
}