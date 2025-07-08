namespace FinVault.API.Helpers;

public static class AccountNumberGenerator
{
    private static readonly Random _random = new();

    // generates a 10-digit account number
    public static string Generate()
    {
        var prefix = "FV";
        var digits = string.Empty;
        for (int i = 0; i < 8; i++)
        {
            digits += _random.Next(0, 10).ToString();
        }
        return prefix + digits;
    }
}
