using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Infrastructure.Services;

public class NotificationService(IUnitOfWork unitOfWork, IFcmService fcmService) : INotificationService
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

        var tokens = await unitOfWork.DeviceTokens.FindAsync(
            t => t.UserId == userId && !t.IsDeleted, cancellationToken);
        var fcmTokens = tokens.Select(t => t.Token).ToList();
        if (fcmTokens.Count > 0)
            await fcmService.SendToTokensAsync(fcmTokens, title, message, cancellationToken);
    }

    public async Task SendToRoleAsync(string role, string title, string message,
        NotificationType type, NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var parsedRole = Enum.Parse<UserRole>(role, ignoreCase: true);
        var users = await unitOfWork.Users.FindAsync(
            u => u.Role == parsedRole && u.IsActive && !u.IsDeleted, cancellationToken);

        var userList = users.ToList();
        var notifications = userList.Select(u => new Notification
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

        var userIds = userList.Select(u => u.Id).ToList();
        var tokens = await unitOfWork.DeviceTokens.FindAsync(
            t => userIds.Contains(t.UserId) && !t.IsDeleted, cancellationToken);
        var fcmTokens = tokens.Select(t => t.Token).Distinct().ToList();
        if (fcmTokens.Count > 0)
            await fcmService.SendToTokensAsync(fcmTokens, title, message, cancellationToken);
    }

    public async Task SendBulkAsync(IEnumerable<Guid> userIds, string title, string message,
        NotificationType type, NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var idList = userIds.ToList();
        var now = DateTime.UtcNow;
        foreach (var userId in idList)
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

        var tokens = await unitOfWork.DeviceTokens.FindAsync(
            t => idList.Contains(t.UserId) && !t.IsDeleted, cancellationToken);
        var fcmTokens = tokens.Select(t => t.Token).Distinct().ToList();
        if (fcmTokens.Count > 0)
            await fcmService.SendToTokensAsync(fcmTokens, title, message, cancellationToken);
    }
}
