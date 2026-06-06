using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Infrastructure.Services;

public class NotificationService(IUnitOfWork unitOfWork) : INotificationService
{
    public async Task SendAsync(Guid userId, string title, string message,
        NotificationType type, NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId  = userId,
            Title   = title,
            Message = message,
            Type    = type,
            Event   = notificationEvent,
            IsSent  = true,
            SentAt  = DateTime.UtcNow
        };
        await unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SendToRoleAsync(string role, string title, string message,
        NotificationType type, NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var parsedRole = Enum.Parse<UserRole>(role, ignoreCase: true);
        var users = await unitOfWork.Users.FindAsync(
            u => u.Role == parsedRole && u.IsActive && !u.IsDeleted, cancellationToken);

        var notifications = users.Select(u => new Notification
        {
            UserId  = u.Id,
            Title   = title,
            Message = message,
            Type    = type,
            Event   = notificationEvent,
            IsSent  = true,
            SentAt  = DateTime.UtcNow
        }).ToList();

        foreach (var n in notifications)
            await unitOfWork.Notifications.AddAsync(n, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SendBulkAsync(IEnumerable<Guid> userIds, string title, string message,
        NotificationType type, NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var userId in userIds)
        {
            await unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId  = userId,
                Title   = title,
                Message = message,
                Type    = type,
                Event   = notificationEvent,
                IsSent  = true,
                SentAt  = now
            }, cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
