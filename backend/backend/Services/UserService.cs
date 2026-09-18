using backend.DTOs;
using backend.Repositories;
using backend.Utilities;

namespace backend.Services;

public class UserService : IUserService
{
    private readonly UserRepository _userRepository;
    private readonly JwtUtil _jwtUtil;
    public UserService(UserRepository userRepository, JwtUtil jwtUtil)
    {
        _userRepository = userRepository;
        _jwtUtil = jwtUtil;
    }

    public Task<UserResponseDto> LoginUser(int userId)
    {
        
    }
}