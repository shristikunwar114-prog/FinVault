using FinVault.API.DTOs.User;

namespace FinVault.API.Services.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetProfileAsync(int userId);
    Task<UserResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task DeleteAccountAsync(int userId);
}
