using System.ComponentModel.DataAnnotations;

namespace FinVault.API.DTOs.Transaction;

public class WithdrawalRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = "General";
}
