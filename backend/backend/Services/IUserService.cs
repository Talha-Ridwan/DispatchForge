using backend.DTOs;

namespace backend.Services;

public interface IUserService
{
    Task<UserResponseDto> LoginUser(UserRequestDto userRequestDto);
    Task<UserResponseDto> CreateUser(UserRequestDto userRequestDto);
    Task DeleteUser(UserRequestDto userRequestDto);
}