using FinVault.API.Data;
using FinVault.API.DTOs.Account;
using FinVault.API.DTOs.Transaction;
using FinVault.API.Models;
using FinVault.API.Services;
using FinVault.API.Services.Interfaces;
using FinVault.Tests.Helpers;
using Moq;
using Xunit;

namespace FinVault.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<INotificationService> _notifMock;

    public TransactionServiceTests()
    {
        _notifMock = new Mock<INotificationService>();
        _notifMock.Setup(n => n.CreateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private async Task<(AppDbContext db, int userId, int accountId)> SetupAsync(string dbName, decimal initialBalance = 1000m)
    {
        var db = TestDbContextFactory.Create(dbName);

        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            PasswordHash = "hash",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var accountService = new AccountService(db);
        var account = await accountService.CreateAccountAsync(user.Id, new CreateAccountRequest
        {
            Type = AccountType.Checking,
            InitialDeposit = initialBalance
        });

        return (db, user.Id, account.Id);
    }

    [Fact]
    public async Task Deposit_IncreasesBalance()
    {
        var (db, userId, accountId) = await SetupAsync("tx_deposit");
        var service = new TransactionService(db, _notifMock.Object);

        var result = await service.DepositAsync(userId, accountId, new DepositRequest
        {
            Amount = 500m,
            Description = "Salary"
        });

        Assert.Equal(500m, result.Amount);
        Assert.Equal(1500m, result.BalanceAfter);
        Assert.Equal("Deposit", result.Type);
    }

    [Fact]
    public async Task Withdraw_DecreasesBalance()
    {
        var (db, userId, accountId) = await SetupAsync("tx_withdraw");
        var service = new TransactionService(db, _notifMock.Object);

        var result = await service.WithdrawAsync(userId, accountId, new WithdrawalRequest
        {
            Amount = 200m,
            Description = "Groceries",
            Category = "Food"
        });

        Assert.Equal(200m, result.Amount);
        Assert.Equal(800m, result.BalanceAfter);
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_ThrowsException()
    {
        var (db, userId, accountId) = await SetupAsync("tx_withdraw_insufficient");
        var service = new TransactionService(db, _notifMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.WithdrawAsync(userId, accountId, new WithdrawalRequest { Amount = 9999m }));
    }

    [Fact]
    public async Task Transfer_MovesMoneyBetweenAccounts()
    {
        var db = TestDbContextFactory.Create("tx_transfer");

        var user = new User
        {
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PasswordHash = "hash",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var accountService = new AccountService(db);
        var from = await accountService.CreateAccountAsync(user.Id, new CreateAccountRequest
        {
            Type = AccountType.Checking, InitialDeposit = 1000m
        });
        var to = await accountService.CreateAccountAsync(user.Id, new CreateAccountRequest
        {
            Type = AccountType.Savings, InitialDeposit = 0m
        });

        var txService = new TransactionService(db, _notifMock.Object);
        var (outTx, inTx) = await txService.TransferAsync(user.Id, new TransferRequest
        {
            FromAccountId = from.Id,
            ToAccountId = to.Id,
            Amount = 300m,
            Description = "Savings"
        });

        Assert.Equal(700m, outTx.BalanceAfter);
        Assert.Equal(300m, inTx.BalanceAfter);
    }

    [Fact]
    public async Task Transfer_ToSameAccount_ThrowsException()
    {
        var (db, userId, accountId) = await SetupAsync("tx_transfer_same");
        var service = new TransactionService(db, _notifMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TransferAsync(userId, new TransferRequest
            {
                FromAccountId = accountId,
                ToAccountId = accountId,
                Amount = 100m
            }));
    }

    [Fact]
    public async Task Deposit_LargeAmount_FlagsTransaction()
    {
        var (db, userId, accountId) = await SetupAsync("tx_fraud_flag");
        var service = new TransactionService(db, _notifMock.Object);

        // deposit more than the fraud threshold (10000)
        var result = await service.DepositAsync(userId, accountId, new DepositRequest
        {
            Amount = 15000m
        });

        Assert.True(result.IsFlagged);
        Assert.Equal("Flagged", result.Status);
        Assert.NotEmpty(result.FlagReason!);
    }

    [Fact]
    public async Task GetTransactions_FiltersByDateRange()
    {
        var (db, userId, accountId) = await SetupAsync("tx_filter_date", 5000m);
        var service = new TransactionService(db, _notifMock.Object);

        // add a few transactions
        await service.DepositAsync(userId, accountId, new DepositRequest { Amount = 100m });
        await service.WithdrawAsync(userId, accountId, new WithdrawalRequest { Amount = 50m });

        var result = await service.GetTransactionsAsync(userId, accountId, new TransactionFilterRequest
        {
            From = DateTime.UtcNow.AddDays(-1),
            To = DateTime.UtcNow.AddDays(1),
            Page = 1,
            PageSize = 10
        });

        // initial deposit tx + 2 more = 3
        Assert.True(result.TotalCount >= 2);
    }

    [Fact]
    public async Task GetSpendingAnalytics_GroupsByCategory()
    {
        var (db, userId, accountId) = await SetupAsync("tx_analytics", 5000m);
        var service = new TransactionService(db, _notifMock.Object);

        await service.WithdrawAsync(userId, accountId, new WithdrawalRequest
        {
            Amount = 100m, Category = "Food"
        });
        await service.WithdrawAsync(userId, accountId, new WithdrawalRequest
        {
            Amount = 200m, Category = "Food"
        });
        await service.WithdrawAsync(userId, accountId, new WithdrawalRequest
        {
            Amount = 500m, Category = "Rent"
        });

        var analytics = await service.GetSpendingAnalyticsAsync(
            userId, accountId, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        Assert.Equal(300m, analytics["Food"]);
        Assert.Equal(500m, analytics["Rent"]);
    }
}
