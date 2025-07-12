using FinVault.API.Models;

namespace FinVault.API.Services.Interfaces;

public interface INotificationService
{
    Task<List<Notification>> GetNotificationsAsync(int userId, bool unreadOnly = false);
    Task MarkReadAsync(int userId, int notificationId);
    Task MarkAllReadAsync(int userId);
    Task DeleteAsync(int userId, int notificationId);
    Task CreateAsync(int userId, string title, string message);
}
