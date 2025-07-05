using FinVault.API.Models;

namespace FinVault.API.DTOs.Transaction;

public class TransactionFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public TransactionType? Type { get; set; }
    public string? Category { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool? IsFlagged { get; set; }
}
