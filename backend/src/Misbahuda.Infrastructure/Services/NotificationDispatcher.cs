using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Infrastructure.Services;

/// <summary>
/// Sends in-app notification + SignalR push + optional WhatsApp + optional Email.
/// </summary>
public class NotificationDispatcher(
    IUnitOfWork unitOfWork,
    IWhatsAppService whatsApp,
    IEmailService email,
    IRealtimePusher pusher)
{
    public async Task SendAsync(
        User user,
        string title,
        string message,
        NotificationEvent notifEvent,
        bool sendWhatsApp = false,
        bool sendEmail = false,
        CancellationToken cancellationToken = default)
    {
        var notif = new Notification
        {
            UserId  = user.Id,
            Title   = title,
            Message = message,
            Type    = NotificationType.Push,
            Event   = notifEvent
        };
        await unitOfWork.Notifications.AddAsync(notif, cancellationToken);

        _ = pusher.PushToUserAsync(user.Id.ToString(), "NewNotification", BuildPayload(notif));

        if (sendWhatsApp && !string.IsNullOrEmpty(user.WhatsAppNumber))
            _ = whatsApp.SendMessageAsync(user.WhatsAppNumber, $"*{title}*\n\n{message}\n\n— Misbahuda Team 🕌");

        if (sendEmail && !string.IsNullOrEmpty(user.Email))
            _ = email.SendAsync(user.Email, user.FullName, title, BuildEmailHtml(title, message));
    }

    public async Task BroadcastAsync(
        IEnumerable<User> users,
        string title,
        string message,
        NotificationEvent notifEvent,
        bool sendWhatsApp = false,
        bool sendEmail = false,
        CancellationToken cancellationToken = default)
    {
        var userList = users.ToList();

        foreach (var user in userList)
        {
            var notif = new Notification
            {
                UserId  = user.Id,
                Title   = title,
                Message = message,
                Type    = NotificationType.Push,
                Event   = notifEvent
            };
            await unitOfWork.Notifications.AddAsync(notif, cancellationToken);
            _ = pusher.PushToUserAsync(user.Id.ToString(), "NewNotification", BuildPayload(notif));
        }

        if (sendWhatsApp)
        {
            var phones = userList.Where(u => !string.IsNullOrEmpty(u.WhatsAppNumber))
                                 .Select(u => u.WhatsAppNumber!).ToList();
            if (phones.Any())
                _ = whatsApp.BroadcastAsync(phones, $"*{title}*\n\n{message}\n\n— Misbahuda Team 🕌");
        }

        if (sendEmail)
        {
            var html = BuildEmailHtml(title, message);
            var recipients = userList.Where(u => !string.IsNullOrEmpty(u.Email))
                                     .Select(u => (u.Email!, u.FullName)).ToList();
            if (recipients.Any())
                _ = email.BroadcastAsync(recipients, title, html);
        }
    }

    private static object BuildPayload(Notification n) => new
    {
        id        = n.Id,
        title     = n.Title,
        message   = n.Message,
        type      = n.Type.ToString(),
        @event    = n.Event.ToString(),
        isRead    = false,
        createdAt = n.CreatedAt
    };

    private static string BuildEmailHtml(string title, string message) => $"""
        <!DOCTYPE html>
        <html>
        <body style="margin:0;padding:0;background:#020617;font-family:Arial,sans-serif;">
          <div style="max-width:600px;margin:0 auto;padding:20px;">
            <div style="background:linear-gradient(135deg,#0a1208,#0f172a);border:1px solid rgba(180,83,9,0.4);border-radius:12px;overflow:hidden;">
              <div style="background:linear-gradient(90deg,transparent,#d97706,transparent);height:3px;"></div>
              <div style="padding:32px;">
                <div style="text-align:center;margin-bottom:24px;">
                  <h1 style="color:#d97706;font-size:22px;margin:0;">Misbah ul Hoda</h1>
                  <p style="color:#64748b;font-size:13px;margin:4px 0 0;">Arbaeen 2026</p>
                </div>
                <h2 style="color:#f1f5f9;font-size:18px;margin:0 0 16px;">{title}</h2>
                <p style="color:#94a3b8;font-size:15px;line-height:1.7;white-space:pre-wrap;">{message}</p>
                <div style="margin-top:28px;padding-top:20px;border-top:1px solid rgba(255,255,255,0.1);text-align:center;">
                  <p style="color:#475569;font-size:12px;margin:0;">Misbahuda — Arbaeen 2026 Pilgrimage Management System</p>
                  <p style="color:#334155;font-size:11px;margin:4px 0 0;">This is an automated message. Please do not reply.</p>
                </div>
              </div>
            </div>
          </div>
        </body>
        </html>
        """;
}
