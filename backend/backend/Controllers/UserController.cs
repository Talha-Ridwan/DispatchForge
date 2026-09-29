using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }
    
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser(UserRequestDto userRequestDto)
    {
        UserResponseDto registeredUser = await _userService.CreateUser(userRequestDto);
        return Ok(registeredUser);
    }
    
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> LoginUser(UserLoginRequestDto userLoginRequestDto)
    {
        UserResponseDto? userResponseDto = await _userService.LoginUser(userLoginRequestDto);
        return userResponseDto == null ? Unauthorized() : Ok(userResponseDto);
    }
}