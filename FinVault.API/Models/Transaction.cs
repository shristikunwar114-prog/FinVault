namespace FinVault.API.Models;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    TransferOut,
    TransferIn
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Flagged
}

public class Transaction
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    public bool IsFlagged { get; set; }
    public string? FlagReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    // for transfers, stores the other account id
    public int? RelatedAccountId { get; set; }
}
