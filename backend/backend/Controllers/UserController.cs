using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser(UserRequestDto userRequestDto)
    {
        UserResponseDto registeredUser = await _userService.CreateUser(userRequestDto);
        return Ok(registeredUser);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUser(UserRequestDto userRequestDto)
    {
        UserResponseDto userResponseDto = await _userService.LoginUser(userRequestDto);
        return Ok(userResponseDto);
    }
}