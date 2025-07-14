using FinVault.API.DTOs.Common;
using FinVault.API.DTOs.Transaction;

namespace FinVault.API.Services.Interfaces;

public interface ITransactionService
{
    Task<TransactionResponse> DepositAsync(int userId, int accountId, DepositRequest request);
    Task<TransactionResponse> WithdrawAsync(int userId, int accountId, WithdrawalRequest request);
    Task<(TransactionResponse outTx, TransactionResponse inTx)> TransferAsync(int userId, TransferRequest request);
    Task<PagedResult<TransactionResponse>> GetTransactionsAsync(int userId, int accountId, TransactionFilterRequest filter);
    Task<TransactionResponse> GetTransactionAsync(int userId, int transactionId);
    Task<Dictionary<string, decimal>> GetSpendingAnalyticsAsync(int userId, int accountId, int year, int month);
}
