using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Infrastructure.Services;

/// <summary>
/// Runs every 5 minutes. When a Majalis is ~30 minutes away, sends
/// an in-app notification + email to every approved pilgrim — once per Majalis.
/// </summary>
public class MajalisReminderService(IServiceScopeFactory scopeFactory, ILogger<MajalisReminderService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("MajalisReminderService started. Checks every 5 minutes.");

        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromMinutes(5), ct); }
            catch (OperationCanceledException) { break; }

            try { await CheckAndSendRemindersAsync(ct); }
            catch (Exception ex) { logger.LogError(ex, "MajalisReminderService error"); }
        }
    }

    private async Task CheckAndSendRemindersAsync(CancellationToken ct)
    {
        using var scope        = scopeFactory.CreateScope();
        var uow                = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var email              = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notificationSvc    = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now         = DateTime.UtcNow;
        var windowStart = now.AddMinutes(25);   // window: 25–35 min ahead
        var windowEnd   = now.AddMinutes(35);

        // Active majalis starting in ~30 min that have NOT been reminded yet
        var upcoming = (await uow.Majalis.FindAsync(
            m => m.IsActive && m.ReminderSentAt == null &&
                 m.StartTime >= windowStart && m.StartTime <= windowEnd, ct)).ToList();

        if (!upcoming.Any()) return;

        // All approved pilgrims
        var pilgrims = (await uow.Pilgrims.FindAsync(
            p => !p.IsDeleted && p.Status == ApplicationStatus.Approved, ct)).ToList();

        var userIds = pilgrims.Select(p => p.UserId).Distinct().ToList();

        bool anyChanged = false;

        foreach (var majalis in upcoming)
        {
            logger.LogInformation("Sending 30-min reminder for: {Title} at {Time}",
                majalis.Title, majalis.StartTime);

            var timeStr = majalis.StartTime.ToString("hh:mm tt") + " UTC";
            var title   = $"📿 Majalis 30 Min Mein!";
            var message = $"{majalis.Title} — {timeStr} @ {majalis.Venue}";

            // Bulk in-app notification to all approved pilgrims
            if (userIds.Any())
                await notificationSvc.SendBulkAsync(
                    userIds, title, message,
                    NotificationType.Push, NotificationEvent.MajalisReminder, ct);

            // Email each pilgrim individually (so we get their name in the email)
            var users = (await uow.Users.FindAsync(u => userIds.Contains(u.Id) && u.IsActive, ct)).ToList();
            var emailSubject = $"📿 {majalis.Title} — Shuru Honay Wali Hai! (30 min)";

            foreach (var user in users)
            {
                var html = BuildReminderEmail(user.FullName, majalis);
                await email.SendAsync(user.Email, user.FullName, emailSubject, html);
            }

            majalis.ReminderSentAt = now;
            uow.Majalis.Update(majalis);
            anyChanged = true;
        }

        if (anyChanged) await uow.SaveChangesAsync(ct);
    }

    private static string BuildReminderEmail(string name, Domain.Entities.Majalis m)
    {
        var start  = m.StartTime.ToString("hh:mm tt") + " UTC";
        var end    = m.EndTime.ToString("hh:mm tt") + " UTC";
        var imgHtml = !string.IsNullOrEmpty(m.ImageUrl)
            ? $"""<img src="{m.ImageUrl}" alt="Scholar" style="width:100%;max-height:240px;object-fit:cover;border-radius:10px;margin-bottom:18px"/>"""
            : "";

        return $"""
            <div style="font-family:sans-serif;max-width:520px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:14px;border:1px solid rgba(124,58,237,0.4)">
              {imgHtml}
              <h2 style="color:#a78bfa;margin-top:0">📿 Majalis 30 Minutes Mein Shuru Ho Gi!</h2>
              <p>Assalam-o-Alaikum <strong>{name}</strong>,</p>
              <p style="color:#94a3b8">Tayar ho jayen — aaj ki Majalis 30 minutes mein shuru hone wali hai.</p>
              <div style="background:#1e293b;border-radius:10px;padding:18px;margin:16px 0;border-left:4px solid #7c3aed">
                <p style="font-size:18px;font-weight:bold;color:#c4b5fd;margin:0 0 12px">{m.Title}</p>
                <table style="font-size:14px;color:#cbd5e1;width:100%;border-collapse:collapse">
                  <tr><td style="padding:4px 0;color:#94a3b8;width:100px">Waqt:</td><td style="font-weight:bold;color:#f1f5f9">{start} — {end}</td></tr>
                  <tr><td style="padding:4px 0;color:#94a3b8">Jagah:</td><td style="color:#f1f5f9">{m.Venue}</td></tr>
                  <tr><td style="padding:4px 0;color:#94a3b8">Zaban:</td><td style="color:#f1f5f9">{m.Language}</td></tr>
                  {(m.MolanaName is not null ? $"<tr><td style='padding:4px 0;color:#94a3b8'>Molana:</td><td style='color:#fbbf24;font-weight:bold'>{m.MolanaName}</td></tr>" : "")}
                  {(m.NohaKhuwanName is not null ? $"<tr><td style='padding:4px 0;color:#94a3b8'>Noha Khuwaan:</td><td style='color:#60a5fa'>{m.NohaKhuwanName}</td></tr>" : "")}
                </table>
                {(m.Description is not null ? $"<p style='color:#94a3b8;font-size:13px;margin:12px 0 0;border-top:1px solid #334155;padding-top:10px'>{m.Description}</p>" : "")}
              </div>
              <div style="background:#2e1065;border:1px solid #6d28d9;border-radius:8px;padding:14px 16px;text-align:center;font-size:16px;font-weight:bold;color:#c4b5fd">
                🕌 Tayar Ho Jayen — Majalis 30 Min Mein!
              </div>
              <hr style="border-color:rgba(124,58,237,0.2);margin:22px 0"/>
              <p style="color:#475569;font-size:12px;margin:0">Misbah ul Hoda — Arbaeen 2026 Pilgrimage Management</p>
            </div>
            """;
    }
}
