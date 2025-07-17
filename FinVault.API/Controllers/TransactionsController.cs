using System.Security.Claims;
using FinVault.API.DTOs.Common;
using FinVault.API.DTOs.Transaction;
using FinVault.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinVault.API.Controllers;

[ApiController]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("api/accounts/{accountId}/deposit")]
    public async Task<IActionResult> Deposit(int accountId, [FromBody] DepositRequest request)
    {
        var userId = GetUserId();
        var result = await _transactionService.DepositAsync(userId, accountId, request);
        return Ok(ApiResponse<TransactionResponse>.Ok(result, "Deposit successful."));
    }

    [HttpPost("api/accounts/{accountId}/withdraw")]
    public async Task<IActionResult> Withdraw(int accountId, [FromBody] WithdrawalRequest request)
    {
        var userId = GetUserId();
        var result = await _transactionService.WithdrawAsync(userId, accountId, request);
        return Ok(ApiResponse<TransactionResponse>.Ok(result, "Withdrawal successful."));
    }

    [HttpPost("api/transfers")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var userId = GetUserId();
        var (outTx, _) = await _transactionService.TransferAsync(userId, request);
        return Ok(ApiResponse<TransactionResponse>.Ok(outTx, "Transfer successful."));
    }

    [HttpGet("api/accounts/{accountId}/transactions")]
    public async Task<IActionResult> GetTransactions(int accountId, [FromQuery] TransactionFilterRequest filter)
    {
        var userId = GetUserId();
        var result = await _transactionService.GetTransactionsAsync(userId, accountId, filter);
        return Ok(ApiResponse<PagedResult<TransactionResponse>>.Ok(result));
    }

    [HttpGet("api/transactions/{id}")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var userId = GetUserId();
        var result = await _transactionService.GetTransactionAsync(userId, id);
        return Ok(ApiResponse<TransactionResponse>.Ok(result));
    }

    [HttpGet("api/accounts/{accountId}/analytics")]
    public async Task<IActionResult> GetSpendingAnalytics(
        int accountId, [FromQuery] int year, [FromQuery] int month)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        if (month == 0) month = DateTime.UtcNow.Month;

        var userId = GetUserId();
        var result = await _transactionService.GetSpendingAnalyticsAsync(userId, accountId, year, month);
        return Ok(ApiResponse<Dictionary<string, decimal>>.Ok(result));
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim!);
    }
}
