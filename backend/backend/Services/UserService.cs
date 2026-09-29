using backend.DTOs;
using backend.Entities;
using backend.Mappers;
using backend.Repositories;
using backend.Utils;
using Microsoft.AspNetCore.Identity;

namespace backend.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtUtil _jwtUtil;
    private readonly UserMapper _userMapper;
    private readonly IPasswordHasher<User> _passwordHasher;
    public UserService(IUserRepository userRepository,
        JwtUtil jwtUtil,
        UserMapper userMapper,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _jwtUtil = jwtUtil;
        _userMapper = userMapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponseDto?> LoginUser(UserLoginRequestDto userLoginRequestDto)
    {
        var queriedUser = await _userRepository.GetUserByUsername(userLoginRequestDto.Username);
        if (queriedUser == null ||
            _passwordHasher.VerifyHashedPassword(queriedUser, queriedUser.Password, userLoginRequestDto.Password)
                == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var token = _jwtUtil.CreateToken(queriedUser);

        var response = _userMapper.ToResponse(queriedUser, token);
        return response;
    }

    public async Task<UserResponseDto> CreateUser(UserRequestDto userRequestDto)
    {
        var user = new User();
        user.Role = userRequestDto.Role;
        user.Username = userRequestDto.Username;
        user.Email = userRequestDto.Email;
        user.Password = _passwordHasher.HashPassword(user, userRequestDto.Password);

        return _userMapper.ToResponse(await _userRepository.SaveUser(user), string.Empty);
    }

    public async Task DeleteUser(UserRequestDto userRequestDto) //2 round trips but user deletion is not a high concurrency task here so we can let it slide
    {
        var user =  await _userRepository.GetUserByUsername(userRequestDto.Username);
        if(user is null) return;
        await _userRepository.DeleteUser(user.Id);
    }
}