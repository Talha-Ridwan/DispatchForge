using backend.DTOs;
using backend.Mappers;
using backend.Repositories;
using backend.Services;
using backend.Utilities;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using backend.Entities;              // User
using Microsoft.AspNetCore.Identity; // PasswordHasher<T>
namespace backend.Tests.Services;

public class UserServiceTests
{

    private readonly IUserRepository _userRepository;
    private readonly UserService _sut; //accr for system under test
    private readonly IConfiguration _config = new ConfigurationBuilder().
        AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "32323232323232323232323232323232",
            ["Jwt:Issuer"] = "what do I write here?",
            ["Jwt:Audience"] = "Uhh, hello everyone?",
        })
        .Build();
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();
    
    
    public UserServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new UserService(_userRepository, new JwtUtil(_config), new UserMapper(), _hasher);
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