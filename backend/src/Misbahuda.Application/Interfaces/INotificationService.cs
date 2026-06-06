using Misbahuda.Domain.Enums;

namespace Misbahuda.Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string message, NotificationType type, NotificationEvent notificationEvent, CancellationToken cancellationToken = default);
    Task SendToRoleAsync(string role, string title, string message, NotificationType type, NotificationEvent notificationEvent, CancellationToken cancellationToken = default);
    Task SendBulkAsync(IEnumerable<Guid> userIds, string title, string message, NotificationType type, NotificationEvent notificationEvent, CancellationToken cancellationToken = default);
}
