using backend.DTOs;
using backend.Mappers;
using backend.Repositories;
using backend.Services;
using backend.Utils;
using backend.Settings;
using Microsoft.Extensions.Options;
using NSubstitute;
using backend.Entities;
using Microsoft.AspNetCore.Identity;
namespace backend.Tests.Services;

public class UserServiceTests
{

    private readonly IUserRepository _userRepository;
    private readonly UserService _sut; //accr for system under test

    private readonly JwtSettings _jwtSettings = new()
    {
        Key = "32323232323232323232323232323232",
        Issuer = "What do I write here?",
        Audience = "Umm, Hello?"
    };
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();
    
    
    public UserServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new UserService(_userRepository, new JwtUtil(Options.Create(_jwtSettings)), new UserMapper(), _hasher);
    }
    
    
    [Fact]
    public async Task CreateUser_ValidRequest_ReturnsCreatedUser()
    {
        UserRequestDto reqdto = new UserRequestDto();
        reqdto.Username = "John Doe";
        reqdto.Password = "itookapillinibizia";
        reqdto.Email = "supervalid@email.com";
        reqdto.Role = "Admin";
        
        _userRepository.SaveUser(Arg.Any<User>()).Returns(callInfo =>
        {
            var savedUser = callInfo.Arg<User>();
            savedUser.Id = 1;
            return savedUser;
        });

        UserResponseDto respdto = new UserResponseDto()
        {
            Email = "supervalid@gmail.com",
            Username = "John Doe",
            Role = "Admin",
            JwtToken = ""
        };

        var result = await _sut.CreateUser(reqdto);
        
        Assert.Equal(reqdto.Username, result.Username);
        Assert.Equal(reqdto.Role, result.Role);
        Assert.Equal(reqdto.Email, result.Email);
        Assert.Empty(result.JwtToken);
        Assert.Equal(1, result.Id);
        await _userRepository.Received(1).SaveUser(Arg.Is<User>(u => u.Password != reqdto.Password));
    }
}