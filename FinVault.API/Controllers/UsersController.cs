using System.Security.Claims;
using FinVault.API.DTOs.Common;
using FinVault.API.DTOs.User;
using FinVault.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var result = await _userService.GetProfileAsync(userId);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var result = await _userService.UpdateProfileAsync(userId, request);
        return Ok(ApiResponse<UserResponse>.Ok(result, "Profile updated."));
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        await _userService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse<object>.Ok(null!, "Password changed successfully."));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();
        await _userService.DeleteAccountAsync(userId);
        return Ok(ApiResponse<object>.Ok(null!, "Account deactivated."));
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim!);
    }
}
