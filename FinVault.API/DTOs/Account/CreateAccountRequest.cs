using System.ComponentModel.DataAnnotations;
using FinVault.API.Models;

namespace FinVault.API.DTOs.Account;

public class CreateAccountRequest
{
    [Required]
    public AccountType Type { get; set; }

    public string Currency { get; set; } = "USD";

    // optional opening deposit
    [Range(0, double.MaxValue)]
    public decimal InitialDeposit { get; set; }
}
