using FinVault.API.Data;
using FinVault.API.DTOs.Account;
using FinVault.API.Models;
using FinVault.API.Services;
using FinVault.Tests.Helpers;
using Xunit;

namespace FinVault.Tests.Services;

public class AccountServiceTests
{
    private async Task<(AppDbContext db, int userId)> SetupUserAsync(string dbName)
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
        return (db, user.Id);
    }

    [Fact]
    public async Task CreateAccount_WithoutInitialDeposit_StartsAtZero()
    {
        var (db, userId) = await SetupUserAsync("acc_create_no_deposit");
        var service = new AccountService(db);

        var result = await service.CreateAccountAsync(userId, new CreateAccountRequest
        {
            Type = AccountType.Checking,
            InitialDeposit = 0
        });

        Assert.Equal(0, result.Balance);
        Assert.Equal("Checking", result.Type);
        Assert.Equal("Active", result.Status);
        Assert.StartsWith("FV", result.AccountNumber);
    }

    [Fact]
    public async Task CreateAccount_WithInitialDeposit_SetsBalance()
    {
        var (db, userId) = await SetupUserAsync("acc_create_with_deposit");
        var service = new AccountService(db);

        var result = await service.CreateAccountAsync(userId, new CreateAccountRequest
        {
            Type = AccountType.Savings,
            InitialDeposit = 500m
        });

        Assert.Equal(500m, result.Balance);
        // should also create a transaction record
        var txCount = db.Transactions.Count();
        Assert.Equal(1, txCount);
    }

    [Fact]
    public async Task GetAccounts_ReturnsOnlyUserAccounts()
    {
        var (db, userId) = await SetupUserAsync("acc_get_user_only");

        // another user
        var otherUser = new User
        {
            FirstName = "Other",
            LastName = "User",
            Email = "other@example.com",
            PasswordHash = "hash",
            PhoneNumber = "9999999999",
            DateOfBirth = new DateTime(1985, 5, 5)
        };
        db.Users.Add(otherUser);
        await db.SaveChangesAsync();

        var service = new AccountService(db);

        await service.CreateAccountAsync(userId, new CreateAccountRequest { Type = AccountType.Checking });
        await service.CreateAccountAsync(otherUser.Id, new CreateAccountRequest { Type = AccountType.Savings });

        var accounts = await service.GetAccountsAsync(userId);

        Assert.Single(accounts);
    }

    [Fact]
    public async Task FreezeAccount_TogglesStatus()
    {
        var (db, userId) = await SetupUserAsync("acc_freeze");
        var service = new AccountService(db);

        var account = await service.CreateAccountAsync(userId, new CreateAccountRequest
        {
            Type = AccountType.Checking
        });

        // freeze it
        var frozen = await service.FreezeAccountAsync(userId, account.Id);
        Assert.Equal("Frozen", frozen.Status);

        // unfreeze it
        var unfrozen = await service.FreezeAccountAsync(userId, account.Id);
        Assert.Equal("Active", unfrozen.Status);
    }

    [Fact]
    public async Task CloseAccount_WithBalance_ThrowsException()
    {
        var (db, userId) = await SetupUserAsync("acc_close_with_balance");
        var service = new AccountService(db);

        var account = await service.CreateAccountAsync(userId, new CreateAccountRequest
        {
            Type = AccountType.Checking,
            InitialDeposit = 100m
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CloseAccountAsync(userId, account.Id));
    }

    [Fact]
    public async Task CloseAccount_WithZeroBalance_Succeeds()
    {
        var (db, userId) = await SetupUserAsync("acc_close_empty");
        var service = new AccountService(db);

        var account = await service.CreateAccountAsync(userId, new CreateAccountRequest
        {
            Type = AccountType.Checking,
            InitialDeposit = 0
        });

        await service.CloseAccountAsync(userId, account.Id);

        // should not show up in list anymore
        var accounts = await service.GetAccountsAsync(userId);
        Assert.Empty(accounts);
    }

    [Fact]
    public async Task GetAccount_WrongUser_ThrowsNotFound()
    {
        var (db, userId) = await SetupUserAsync("acc_get_wrong_user");
        var service = new AccountService(db);

        var account = await service.CreateAccountAsync(userId, new CreateAccountRequest
        {
            Type = AccountType.Checking
        });

        // try to access with wrong user id
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetAccountAsync(userId + 999, account.Id));
    }
}
