using FinVault.API.Data;
using FinVault.API.DTOs.Common;
using FinVault.API.DTOs.Transaction;
using FinVault.API.Models;
using FinVault.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinVault.API.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    // flag transactions over this amount
    private const decimal FraudThreshold = 10000m;

    // flag if user does more than 5 transactions in 1 minute
    private const int RapidTxLimit = 5;

    public TransactionService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<TransactionResponse> DepositAsync(int userId, int accountId, DepositRequest request)
    {
        var account = await GetActiveAccountAsync(userId, accountId);

        account.Balance += request.Amount;

        var tx = new Transaction
        {
            AccountId = accountId,
            Type = TransactionType.Deposit,
            Amount = request.Amount,
            BalanceAfter = account.Balance,
            Description = request.Description,
            Category = request.Category,
            Status = TransactionStatus.Completed,
            ReferenceNumber = GenerateRef(),
            CreatedAt = DateTime.UtcNow
        };

        CheckFraud(tx, userId);

        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        if (tx.IsFlagged)
        {
            await _notificationService.CreateAsync(userId,
                "Transaction Flagged",
                $"Your deposit of {request.Amount:C} has been flagged for review. Ref: {tx.ReferenceNumber}");
        }
        else
        {
            await _notificationService.CreateAsync(userId,
                "Deposit Successful",
                $"{request.Amount:C} deposited to account {account.AccountNumber}. Balance: {account.Balance:C}");
        }

        return MapToResponse(tx);
    }

    public async Task<TransactionResponse> WithdrawAsync(int userId, int accountId, WithdrawalRequest request)
    {
        var account = await GetActiveAccountAsync(userId, accountId);

        if (account.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient funds.");

        account.Balance -= request.Amount;

        var tx = new Transaction
        {
            AccountId = accountId,
            Type = TransactionType.Withdrawal,
            Amount = request.Amount,
            BalanceAfter = account.Balance,
            Description = request.Description,
            Category = request.Category,
            Status = TransactionStatus.Completed,
            ReferenceNumber = GenerateRef(),
            CreatedAt = DateTime.UtcNow
        };

        CheckFraud(tx, userId);

        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        if (tx.IsFlagged)
        {
            await _notificationService.CreateAsync(userId,
                "Transaction Flagged",
                $"Your withdrawal of {request.Amount:C} has been flagged for review. Ref: {tx.ReferenceNumber}");
        }
        else
        {
            await _notificationService.CreateAsync(userId,
                "Withdrawal Successful",
                $"{request.Amount:C} withdrawn from account {account.AccountNumber}. Balance: {account.Balance:C}");
        }

        return MapToResponse(tx);
    }

    public async Task<(TransactionResponse outTx, TransactionResponse inTx)> TransferAsync(int userId, TransferRequest request)
    {
        if (request.FromAccountId == request.ToAccountId)
            throw new InvalidOperationException("Cannot transfer to the same account.");

        var fromAccount = await GetActiveAccountAsync(userId, request.FromAccountId);

        // destination can be any active account (could be another user's)
        var toAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.ToAccountId && a.Status == AccountStatus.Active)
            ?? throw new KeyNotFoundException("Destination account not found or inactive.");

        if (fromAccount.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient funds.");

        fromAccount.Balance -= request.Amount;
        toAccount.Balance += request.Amount;

        var ref1 = GenerateRef();

        var outTx = new Transaction
        {
            AccountId = fromAccount.Id,
            Type = TransactionType.TransferOut,
            Amount = request.Amount,
            BalanceAfter = fromAccount.Balance,
            Description = request.Description,
            Category = "Transfer",
            Status = TransactionStatus.Completed,
            ReferenceNumber = ref1,
            RelatedAccountId = toAccount.Id,
            CreatedAt = DateTime.UtcNow
        };

        var inTx = new Transaction
        {
            AccountId = toAccount.Id,
            Type = TransactionType.TransferIn,
            Amount = request.Amount,
            BalanceAfter = toAccount.Balance,
            Description = request.Description,
            Category = "Transfer",
            Status = TransactionStatus.Completed,
            ReferenceNumber = ref1,
            RelatedAccountId = fromAccount.Id,
            CreatedAt = DateTime.UtcNow
        };

        CheckFraud(outTx, userId);

        _db.Transactions.AddRange(outTx, inTx);
        await _db.SaveChangesAsync();

        await _notificationService.CreateAsync(userId,
            "Transfer Complete",
            $"Sent {request.Amount:C} from {fromAccount.AccountNumber} to {toAccount.AccountNumber}. Ref: {ref1}");

        // notify receiver if they are a different user
        if (toAccount.UserId != userId)
        {
            await _notificationService.CreateAsync(toAccount.UserId,
                "Transfer Received",
                $"You received {request.Amount:C} in account {toAccount.AccountNumber}. Ref: {ref1}");
        }

        return (MapToResponse(outTx), MapToResponse(inTx));
    }

    public async Task<PagedResult<TransactionResponse>> GetTransactionsAsync(
        int userId, int accountId, TransactionFilterRequest filter)
    {
        // make sure account belongs to user
        var accountExists = await _db.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId);

        if (!accountExists)
            throw new KeyNotFoundException("Account not found.");

        var query = _db.Transactions
            .Where(t => t.AccountId == accountId)
            .AsQueryable();

        if (filter.Type.HasValue)
            query = query.Where(t => t.Type == filter.Type.Value);

        if (!string.IsNullOrEmpty(filter.Category))
            query = query.Where(t => t.Category == filter.Category);

        if (filter.From.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.To.Value);

        if (filter.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= filter.MaxAmount.Value);

        if (filter.IsFlagged.HasValue)
            query = query.Where(t => t.IsFlagged == filter.IsFlagged.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<TransactionResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<TransactionResponse> GetTransactionAsync(int userId, int transactionId)
    {
        // join to verify user owns the account
        var tx = await _db.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Account.UserId == userId)
            ?? throw new KeyNotFoundException("Transaction not found.");

        return MapToResponse(tx);
    }

    public async Task<Dictionary<string, decimal>> GetSpendingAnalyticsAsync(
        int userId, int accountId, int year, int month)
    {
        var accountExists = await _db.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId);

        if (!accountExists)
            throw new KeyNotFoundException("Account not found.");

        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        // only look at money going out
        var spending = await _db.Transactions
            .Where(t => t.AccountId == accountId
                && t.CreatedAt >= from && t.CreatedAt < to
                && (t.Type == TransactionType.Withdrawal || t.Type == TransactionType.TransferOut))
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        return spending.ToDictionary(x => x.Category, x => x.Total);
    }

    private async Task<Account> GetActiveAccountAsync(int userId, int accountId)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId)
            ?? throw new KeyNotFoundException("Account not found.");

        if (account.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Account is frozen. Contact support to unfreeze.");

        if (account.Status == AccountStatus.Closed)
            throw new InvalidOperationException("Account is closed.");

        return account;
    }

    private void CheckFraud(Transaction tx, int userId)
    {
        // flag large transactions
        if (tx.Amount >= FraudThreshold)
        {
            tx.IsFlagged = true;
            tx.FlagReason = $"Large transaction exceeds ${FraudThreshold:N0}";
            tx.Status = TransactionStatus.Flagged;
        }
    }

    private static string GenerateRef()
        => "TXN" + Guid.NewGuid().ToString("N")[..10].ToUpper();

    private static TransactionResponse MapToResponse(Transaction t) => new()
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
