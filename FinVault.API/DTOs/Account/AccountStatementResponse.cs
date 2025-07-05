using FinVault.API.DTOs.Transaction;

namespace FinVault.API.DTOs.Account;

public class AccountStatementResponse
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public List<TransactionResponse> Transactions { get; set; } = new();
}
