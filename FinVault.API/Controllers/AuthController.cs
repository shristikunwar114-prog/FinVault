using FinVault.API.DTOs.Auth;
using FinVault.API.DTOs.Common;
using FinVault.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful."));
    }

    // logout is handled client-side by discarding the token
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(ApiResponse<object>.Ok(null!, "Logged out successfully."));
    }
}
