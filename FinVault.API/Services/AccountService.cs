using FinVault.API.Data;
using FinVault.API.DTOs.Account;
using FinVault.API.DTOs.Transaction;
using FinVault.API.Helpers;
using FinVault.API.Models;
using FinVault.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinVault.API.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _db;

    public AccountService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AccountResponse> CreateAccountAsync(int userId, CreateAccountRequest request)
    {
        // generate unique account number
        string accountNumber;
        do
        {
            accountNumber = AccountNumberGenerator.Generate();
        } while (await _db.Accounts.AnyAsync(a => a.AccountNumber == accountNumber));

        var account = new Account
        {
            AccountNumber = accountNumber,
            Type = request.Type,
            Balance = request.InitialDeposit,
            Currency = request.Currency,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        // if there was an initial deposit, record it as a transaction
        if (request.InitialDeposit > 0)
        {
            _db.Transactions.Add(new Transaction
            {
                AccountId = account.Id,
                Type = TransactionType.Deposit,
                Amount = request.InitialDeposit,
                BalanceAfter = request.InitialDeposit,
                Description = "Initial deposit",
                Category = "General",
                Status = TransactionStatus.Completed,
                ReferenceNumber = Guid.NewGuid().ToString("N")[..12].ToUpper(),
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return MapToResponse(account);
    }

    public async Task<List<AccountResponse>> GetAccountsAsync(int userId)
    {
        var accounts = await _db.Accounts
            .Where(a => a.UserId == userId && a.Status != AccountStatus.Closed)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        return accounts.Select(MapToResponse).ToList();
    }

    public async Task<AccountResponse> GetAccountAsync(int userId, int accountId)
    {
        var account = await GetOwnedAccountAsync(userId, accountId);
        return MapToResponse(account);
    }

    public async Task<AccountResponse> FreezeAccountAsync(int userId, int accountId)
    {
        var account = await GetOwnedAccountAsync(userId, accountId);

        if (account.Status == AccountStatus.Closed)
            throw new InvalidOperationException("Cannot freeze a closed account.");

        account.Status = account.Status == AccountStatus.Frozen
            ? AccountStatus.Active
            : AccountStatus.Frozen;

        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToResponse(account);
    }

    public async Task CloseAccountAsync(int userId, int accountId)
    {
        var account = await GetOwnedAccountAsync(userId, accountId);

        if (account.Balance > 0)
            throw new InvalidOperationException("Account must have zero balance before closing.");

        account.Status = AccountStatus.Closed;
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<AccountStatementResponse> GetStatementAsync(int userId, int accountId, int year, int month)
    {
        var account = await GetOwnedAccountAsync(userId, accountId);

        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        var transactions = await _db.Transactions
            .Where(t => t.AccountId == accountId && t.CreatedAt >= from && t.CreatedAt < to)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        // opening balance = balance before the first transaction in this month
        decimal openingBalance = 0;
        if (transactions.Any())
        {
            var firstTx = transactions.First();
            openingBalance = firstTx.BalanceAfter - GetSignedAmount(firstTx);
        }
        else
        {
            openingBalance = account.Balance;
        }

        decimal totalDeposits = transactions
            .Where(t => t.Type == TransactionType.Deposit || t.Type == TransactionType.TransferIn)
            .Sum(t => t.Amount);

        decimal totalWithdrawals = transactions
            .Where(t => t.Type == TransactionType.Withdrawal || t.Type == TransactionType.TransferOut)
            .Sum(t => t.Amount);

        return new AccountStatementResponse
        {
            AccountNumber = account.AccountNumber,
            AccountType = account.Type.ToString(),
            Year = year,
            Month = month,
            OpeningBalance = openingBalance,
            ClosingBalance = account.Balance,
            TotalDeposits = totalDeposits,
            TotalWithdrawals = totalWithdrawals,
            Transactions = transactions.Select(MapTransactionToResponse).ToList()
        };
    }

    // helper to get account that belongs to user
    private async Task<Account> GetOwnedAccountAsync(int userId, int accountId)
    {
        return await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId)
            ?? throw new KeyNotFoundException("Account not found.");
    }

    private static decimal GetSignedAmount(Transaction tx)
    {
        return tx.Type == TransactionType.Deposit || tx.Type == TransactionType.TransferIn
            ? tx.Amount
            : -tx.Amount;
    }

    private static AccountResponse MapToResponse(Account a) => new()
    {
        Id = a.Id,
        AccountNumber = a.AccountNumber,
        Type = a.Type.ToString(),
        Balance = a.Balance,
        Currency = a.Currency,
        Status = a.Status.ToString(),
        CreatedAt = a.CreatedAt
    };

    private static TransactionResponse MapTransactionToResponse(Transaction t) => new()
    {
        Id = t.Id,
        ReferenceNumber = t.ReferenceNumber,
        Type = t.Type.ToString(),
        Amount = t.Amount,
        BalanceAfter = t.BalanceAfter,
        Description = t.Description,
        Category = t.Category,
        Status = t.Status.ToString(),
        IsFlagged = t.IsFlagged,
        FlagReason = t.FlagReason,
        CreatedAt = t.CreatedAt,
        RelatedAccountId = t.RelatedAccountId
    };
}
