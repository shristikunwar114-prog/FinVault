using FinVault.API.DTOs.Auth;

namespace FinVault.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<int> GetUserIdFromTokenAsync(string token);
}
