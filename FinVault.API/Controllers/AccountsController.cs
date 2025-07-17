using System.Security.Claims;
using FinVault.API.DTOs.Account;
using FinVault.API.DTOs.Common;
using FinVault.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var userId = GetUserId();
        var result = await _accountService.CreateAccountAsync(userId, request);
        return CreatedAtAction(nameof(GetAccount), new { id = result.Id },
            ApiResponse<AccountResponse>.Ok(result, "Account created."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        var userId = GetUserId();
        var result = await _accountService.GetAccountsAsync(userId);
        return Ok(ApiResponse<List<AccountResponse>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(int id)
    {
        var userId = GetUserId();
        var result = await _accountService.GetAccountAsync(userId, id);
        return Ok(ApiResponse<AccountResponse>.Ok(result));
    }

    [HttpPut("{id}/freeze")]
    public async Task<IActionResult> ToggleFreeze(int id)
    {
        var userId = GetUserId();
        var result = await _accountService.FreezeAccountAsync(userId, id);
        return Ok(ApiResponse<AccountResponse>.Ok(result, $"Account is now {result.Status}."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CloseAccount(int id)
    {
        var userId = GetUserId();
        await _accountService.CloseAccountAsync(userId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Account closed."));
    }

    [HttpGet("{id}/statement")]
    public async Task<IActionResult> GetStatement(int id, [FromQuery] int year, [FromQuery] int month)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        if (month == 0) month = DateTime.UtcNow.Month;

        var userId = GetUserId();
        var result = await _accountService.GetStatementAsync(userId, id, year, month);
        return Ok(ApiResponse<AccountStatementResponse>.Ok(result));
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim!);
    }
}
