using FinVault.API.DTOs.Account;

namespace FinVault.API.Services.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(int userId, CreateAccountRequest request);
    Task<List<AccountResponse>> GetAccountsAsync(int userId);
    Task<AccountResponse> GetAccountAsync(int userId, int accountId);
    Task<AccountResponse> FreezeAccountAsync(int userId, int accountId);
    Task CloseAccountAsync(int userId, int accountId);
    Task<AccountStatementResponse> GetStatementAsync(int userId, int accountId, int year, int month);
}
