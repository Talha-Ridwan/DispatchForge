using backend.DTOs;

namespace backend.Services;

public interface IUserService
{
    Task<UserResponseDto?> LoginUser(UserLoginRequestDto userLoginRequestDto);
    Task<UserResponseDto> CreateUser(UserRequestDto userRequestDto);
    Task DeleteUser(UserRequestDto userRequestDto);
}