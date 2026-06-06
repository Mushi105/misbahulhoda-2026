using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Infrastructure.Services;

/// <summary>
/// Three reminder schedules:
///   1. Daily at 08:00 UTC — 2-day advance reminder + admin digest
///   2. Every 30 minutes   — 5-hour-before reminder (uses pilgrim's actual flight time)
///   3. Daily at 20:00 UTC — Nightly unassigned check:
///        if admin forgot to assign a driver for tomorrow's travellers,
///        system automatically notifies pilgrim + alerts admin. Once per day.
/// </summary>
public class TravelReminderService(IServiceScopeFactory scopeFactory, ILogger<TravelReminderService> logger)
    : BackgroundService
{
    private DateTime _lastDailyRun   = DateTime.MinValue;
    private DateTime _lastNightlyRun = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("TravelReminderService started. Checks every 30 minutes.");

        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromMinutes(30), ct); }
            catch (OperationCanceledException) { break; }

            var now = DateTime.UtcNow;

            // ── Every 30 min: 5-hour reminders ────────────────────────────
            await Run5HourRemindersAsync(ct);

            // ── 08:00 UTC daily: 2-day reminders + admin digest ───────────
            if (now.Hour == 8 && _lastDailyRun.Date < now.Date)
            {
                _lastDailyRun = now;
                await RunDailyAsync(ct);
            }

            // ── 20:00 UTC daily: nightly unassigned-driver safety check ───
            if (now.Hour == 20 && _lastNightlyRun.Date < now.Date)
            {
                _lastNightlyRun = now;
                await RunNightlyUnassignedCheckAsync(ct);
            }
        }
    }

    // Called from ItineraryController for manual trigger — runs all jobs
    public async Task RunAsync(CancellationToken ct = default)
    {
        await Run5HourRemindersAsync(ct);
        await RunDailyAsync(ct);
        await RunNightlyUnassignedCheckAsync(ct);
    }

    // ── Nightly 20:00 UTC: unassigned-driver check ────────────────────────────
    // If admin forgot to assign a driver for any of tomorrow's travellers,
    // pilgrim gets "we're arranging your transfer" message.
    // Admin gets a WhatsApp + Email alert listing every unassigned traveller.
    private async Task RunNightlyUnassignedCheckAsync(CancellationToken ct)
    {
        logger.LogInformation("Running nightly unassigned-driver check at {Time}", DateTime.UtcNow);
        using var scope  = scopeFactory.CreateScope();
        var uow          = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var email        = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var whatsApp     = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        var tomorrow    = DateTime.UtcNow.Date.AddDays(1);
        var dayAfter    = tomorrow.AddDays(1);

        var arriving  = (await uow.Pilgrims.FindAsync(p => !p.IsDeleted && p.ArrivalDate   >= tomorrow && p.ArrivalDate   < dayAfter, ct)).ToList();
        var departing = (await uow.Pilgrims.FindAsync(p => !p.IsDeleted && p.DepartureDate >= tomorrow && p.DepartureDate < dayAfter, ct)).ToList();

        var allIds   = arriving.Select(p => p.Id).Concat(departing.Select(p => p.Id)).Distinct().ToList();
        if (!allIds.Any()) return;

        var transfers = (await uow.AirportTransfers.FindAsync(t => allIds.Contains(t.PilgrimId), ct)).ToList();
        var userIds   = arriving.Select(p => p.UserId).Concat(departing.Select(p => p.UserId)).Distinct().ToList();
        var users     = (await uow.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);

        // Find those with NO assigned driver
        var unassignedArrivals  = arriving.Where(p =>
            !transfers.Any(t => t.PilgrimId == p.Id && t.TransferType == Domain.Enums.TransferType.Arrival)).ToList();
        var unassignedDepartures = departing.Where(p =>
            !transfers.Any(t => t.PilgrimId == p.Id && t.TransferType == Domain.Enums.TransferType.Departure)).ToList();

        var totalUnassigned = unassignedArrivals.Count + unassignedDepartures.Count;
        logger.LogInformation("Nightly check: {Total} unassigned transfers for tomorrow ({A} arrivals, {D} departures).",
            totalUnassigned, unassignedArrivals.Count, unassignedDepartures.Count);

        if (totalUnassigned == 0) return;

        // ── Notify each unassigned pilgrim ─────────────────────────────────
        foreach (var pilgrim in unassignedArrivals)
        {
            if (!users.TryGetValue(pilgrim.UserId, out var user)) continue;
            var phone = user.WhatsAppNumber ?? user.PhoneNumber;
            var wa = BuildUnassignedPilgrimWhatsApp(user.FullName, pilgrim.ArrivalDate, pilgrim.ArrivalTime, pilgrim.ArrivalFlight, isDeparture: false);
            var html = BuildUnassignedPilgrimEmail(user.FullName, pilgrim.ArrivalDate, pilgrim.ArrivalTime, pilgrim.ArrivalFlight, isDeparture: false);
            if (!string.IsNullOrEmpty(phone)) await whatsApp.SendMessageAsync(phone, wa);
            await email.SendAsync(user.Email, user.FullName, "🛬 Your arrival is tomorrow — Misbah ul Hoda", html);
        }

        foreach (var pilgrim in unassignedDepartures)
        {
            if (!users.TryGetValue(pilgrim.UserId, out var user)) continue;
            var phone = user.WhatsAppNumber ?? user.PhoneNumber;
            var wa = BuildUnassignedPilgrimWhatsApp(user.FullName, pilgrim.DepartureDate, pilgrim.DepartureTime, pilgrim.DepartureFlight, isDeparture: true);
            var html = BuildUnassignedPilgrimEmail(user.FullName, pilgrim.DepartureDate, pilgrim.DepartureTime, pilgrim.DepartureFlight, isDeparture: true);
            if (!string.IsNullOrEmpty(phone)) await whatsApp.SendMessageAsync(phone, wa);
            await email.SendAsync(user.Email, user.FullName, "✈️ Your departure is tomorrow — Misbah ul Hoda", html);
        }

        // ── Alert all admins ───────────────────────────────────────────────
        var adminUsers = (await uow.Users.FindAsync(u => (int)u.Role <= 2 && u.IsActive, ct)).ToList();
        var adminHtml  = BuildAdminUnassignedAlert(tomorrow, unassignedArrivals, unassignedDepartures, users);
        var adminWa    = BuildAdminUnassignedWhatsApp(tomorrow, unassignedArrivals, unassignedDepartures, users);

        foreach (var admin in adminUsers)
        {
            await email.SendAsync(admin.Email, admin.FullName,
                $"⚠️ Action Required: {totalUnassigned} unassigned transfers for {tomorrow:dd MMM yyyy}", adminHtml);
            var adminPhone = admin.WhatsAppNumber ?? admin.PhoneNumber;
            if (!string.IsNullOrEmpty(adminPhone))
                await whatsApp.SendMessageAsync(adminPhone, adminWa);
        }
    }

    private static string BuildUnassignedPilgrimWhatsApp(string name, DateTime date, string? time, string? flight, bool isDeparture)
    {
        var emoji = isDeparture ? "🛫" : "🛬";
        var label = isDeparture ? "departure" : "arrival";
        var timeStr = string.IsNullOrEmpty(time) ? "" : $" at *{time}*";
        var lines = new List<string>
        {
            $"Assalam-o-Alaikum *{name}*! 🕌",
            "",
            "*Misbah ul Hoda — Arbaeen 2026*",
            "",
            $"{emoji} *Your {label} is tomorrow!*",
            $"📅 *{date:dddd, dd MMM yyyy}*{timeStr}",
        };
        if (!string.IsNullOrEmpty(flight)) lines.Add($"✈️ Flight: *{flight}*");
        lines.AddRange([
            "",
            isDeparture
                ? "🎒 *Please prepare your luggage tonight.*"
                : "📋 *Please keep your passport and documents ready.*",
            "",
            "🚗 Our team is *arranging your transfer*. Driver details will be shared with you soon.",
            "📞 If urgent, please contact the admin team directly.",
            "",
            "JazakAllah Khair 🌿",
        ]);
        return string.Join("\n", lines);
    }

    private static string BuildUnassignedPilgrimEmail(string name, DateTime date, string? time, string? flight, bool isDeparture)
    {
        var emoji = isDeparture ? "🛫" : "🛬";
        var label = isDeparture ? "Departure" : "Arrival";
        var color = isDeparture ? "#3b82f6" : "#22c55e";
        return $"""
            <div style="font-family:sans-serif;max-width:520px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:1px solid {color}66">
              <h2 style="color:{color};margin-top:0">{emoji} Your {label.ToLower()} is tomorrow</h2>
              <p>Assalam-o-Alaikum <strong>{name}</strong>,</p>
              <p>This is a reminder that your <strong>{label.ToLower()} is tomorrow</strong>.</p>
              <div style="background:#1e293b;border-radius:8px;padding:14px;margin:14px 0">
                <table style="font-size:14px;color:#cbd5e1;width:100%">
                  <tr><td style="padding:3px 0;color:#94a3b8">Date:</td><td style="font-weight:bold;color:#f1f5f9">{date:dddd, dd MMM yyyy}</td></tr>
                  {(time   is not null ? $"<tr><td style='color:#94a3b8'>Time:</td><td style='font-weight:bold;color:{color}'>{time}</td></tr>" : "")}
                  {(flight is not null ? $"<tr><td style='color:#94a3b8'>Flight:</td><td style='color:#f1f5f9'>{flight}</td></tr>" : "")}
                </table>
              </div>
              <div style="background:#1e3a2f;border:1px solid #166534;border-radius:8px;padding:12px 16px">
                <p style="color:#4ade80;font-weight:bold;margin:0 0 6px">🚗 Transfer Status</p>
                <p style="color:#d1fae5;margin:0;font-size:13px">Our team is arranging your {(isDeparture ? "pickup" : "reception")}. Driver details will be shared with you shortly.</p>
                <p style="color:#94a3b8;margin:6px 0 0;font-size:12px">If you need immediate assistance, please contact the admin team.</p>
              </div>
              <hr style="border-color:rgba(59,130,246,0.2);margin:20px 0"/>
              <p style="color:#475569;font-size:12px">Misbah ul Hoda — Arbaeen 2026 Pilgrimage Management</p>
            </div>
            """;
    }

    private static string BuildAdminUnassignedWhatsApp(
        DateTime tomorrow,
        List<Domain.Entities.Pilgrim> arrivals, List<Domain.Entities.Pilgrim> departures,
        Dictionary<Guid, Domain.Entities.User> users)
    {
        var total = arrivals.Count + departures.Count;
        var lines = new List<string>
        {
            "⚠️ *ACTION REQUIRED — Misbahuda*",
            "",
            $"*{total} pilgrim(s) have NO driver assigned for tomorrow ({tomorrow:dd MMM yyyy})!*",
            "",
        };
        if (arrivals.Any())
        {
            lines.Add($"🛬 *Unassigned Arrivals ({arrivals.Count}):*");
            foreach (var p in arrivals)
            {
                users.TryGetValue(p.UserId, out var u);
                lines.Add($"• {u?.FullName ?? "?"} — {p.ArrivalFlight ?? "no flight"} {(p.ArrivalTime is not null ? "at " + p.ArrivalTime : "")}");
            }
            lines.Add("");
        }
        if (departures.Any())
        {
            lines.Add($"🛫 *Unassigned Departures ({departures.Count}):*");
            foreach (var p in departures)
            {
                users.TryGetValue(p.UserId, out var u);
                lines.Add($"• {u?.FullName ?? "?"} — {p.DepartureFlight ?? "no flight"} {(p.DepartureTime is not null ? "at " + p.DepartureTime : "")}");
            }
            lines.Add("");
        }
        lines.Add("Please assign drivers immediately via the Itinerary panel.");
        return string.Join("\n", lines);
    }

    private static string BuildAdminUnassignedAlert(
        DateTime tomorrow,
        List<Domain.Entities.Pilgrim> arrivals, List<Domain.Entities.Pilgrim> departures,
        Dictionary<Guid, Domain.Entities.User> users)
    {
        string Rows(List<Domain.Entities.Pilgrim> list, bool isDep) => string.Join("", list.Select(p =>
        {
            users.TryGetValue(p.UserId, out var u);
            var flight = isDep ? p.DepartureFlight : p.ArrivalFlight;
            var time   = isDep ? p.DepartureTime   : p.ArrivalTime;
            return $"<tr style='border-bottom:1px solid #1e293b'>" +
                   $"<td style='padding:6px 8px;color:#f1f5f9'>{u?.FullName ?? "?"}</td>" +
                   $"<td style='padding:6px 8px;color:#94a3b8'>{u?.PhoneNumber ?? "—"}</td>" +
                   $"<td style='padding:6px 8px;color:#fbbf24'>{flight ?? "—"}</td>" +
                   $"<td style='padding:6px 8px;color:#94a3b8'>{time ?? "—"}</td>" +
                   $"<td style='padding:6px 8px;color:#f87171;font-weight:bold'>⚠️ Unassigned</td>" +
                   "</tr>";
        }));

        string Table(string title, string color, List<Domain.Entities.Pilgrim> list, bool isDep) =>
            list.Any() ? $"""
                <h3 style="color:{color};margin:20px 0 8px">{title} ({list.Count})</h3>
                <table style="width:100%;border-collapse:collapse;font-size:12px">
                  <thead><tr style="background:#1e293b;color:#64748b">
                    <th style="padding:6px 8px;text-align:left">Name</th>
                    <th style="padding:6px 8px;text-align:left">Phone</th>
                    <th style="padding:6px 8px;text-align:left">Flight</th>
                    <th style="padding:6px 8px;text-align:left">Time</th>
                    <th style="padding:6px 8px;text-align:left">Status</th>
                  </tr></thead>
                  <tbody>{Rows(list, isDep)}</tbody>
                </table>
                """ : "";

        return $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:2px solid #ef444466">
              <h2 style="color:#ef4444;margin-top:0">⚠️ Action Required — Unassigned Transfers</h2>
              <p>The following pilgrims are travelling <strong>tomorrow ({tomorrow:dddd, dd MMM yyyy})</strong> with <strong>no driver assigned</strong>.</p>
              <div style="background:#7f1d1d;border-radius:8px;padding:12px 16px;font-weight:bold;font-size:15px;margin:16px 0">
                Please assign drivers immediately via the <span style="color:#fca5a5">Itinerary panel</span>.
              </div>
              {Table("🛬 Unassigned Arrivals", "#22c55e", arrivals, false)}
              {Table("🛫 Unassigned Departures", "#3b82f6", departures, true)}
              <p style="color:#6b7280;font-size:12px;margin-top:24px">
                This alert was sent automatically at 20:00 UTC by Misbah ul Hoda — Arbaeen 2026 system.
              </p>
            </div>
            """;
    }

    // ── 5-hour reminder ────────────────────────────────────────────────────────
    private async Task Run5HourRemindersAsync(CancellationToken ct)
    {
        using var scope  = scopeFactory.CreateScope();
        var uow          = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var email        = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var whatsApp     = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        var now          = DateTime.UtcNow;
        var today        = now.Date;
        var tomorrow     = today.AddDays(1);
        // Window: pilgrims whose flightTime - 5h falls within [now-15min, now+15min]
        var windowStart  = now.AddMinutes(-15);
        var windowEnd    = now.AddMinutes(15);

        var allPilgrims  = (await uow.Pilgrims.FindAsync(p =>
            !p.IsDeleted && (p.DepartureDate.Date == today || p.DepartureDate.Date == tomorrow ||
                             p.ArrivalDate.Date   == today || p.ArrivalDate.Date   == tomorrow), ct)).ToList();

        if (!allPilgrims.Any()) return;

        var userIds  = allPilgrims.Select(p => p.UserId).ToList();
        var users    = (await uow.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);
        var ids      = allPilgrims.Select(p => p.Id).ToList();
        var transfers = (await uow.AirportTransfers.FindAsync(t => ids.Contains(t.PilgrimId), ct)).ToList();

        bool anyChanged = false;

        foreach (var pilgrim in allPilgrims)
        {
            if (!users.TryGetValue(pilgrim.UserId, out var user)) continue;
            var phone = user.WhatsAppNumber ?? user.PhoneNumber;

            // ── Departure 5h reminder ──
            if (pilgrim.Departure5hReminderSentAt is null)
            {
                var flightDt = ParseFlightDateTime(pilgrim.DepartureDate, pilgrim.DepartureTime);
                var reminderAt = flightDt.AddHours(-5);
                if (reminderAt >= windowStart && reminderAt <= windowEnd)
                {
                    var t = transfers.FirstOrDefault(x => x.PilgrimId == pilgrim.Id && x.TransferType == Domain.Enums.TransferType.Departure);
                    var waMsg = Build5hWhatsApp(user.FullName, pilgrim.DepartureDate, pilgrim.DepartureTime, pilgrim.DepartureFlight, pilgrim.DepartureAirport, t, isDeparture: true);
                    var html  = Build5hEmail(user.FullName, pilgrim.DepartureDate, pilgrim.DepartureTime, pilgrim.DepartureFlight, pilgrim.DepartureAirport, t, isDeparture: true);

                    if (!string.IsNullOrEmpty(phone)) await whatsApp.SendMessageAsync(phone, waMsg);
                    await email.SendAsync(user.Email, user.FullName, "✈️ Your flight departs in 5 hours! — Misbah ul Hoda", html);

                    pilgrim.Departure5hReminderSentAt = now;
                    uow.Pilgrims.Update(pilgrim);
                    anyChanged = true;
                    logger.LogInformation("5h departure reminder → {Name}", user.FullName);
                }
            }

            // ── Arrival 5h reminder ──
            if (pilgrim.Arrival5hReminderSentAt is null)
            {
                var flightDt   = ParseFlightDateTime(pilgrim.ArrivalDate, pilgrim.ArrivalTime);
                var reminderAt = flightDt.AddHours(-5);
                if (reminderAt >= windowStart && reminderAt <= windowEnd)
                {
                    var t = transfers.FirstOrDefault(x => x.PilgrimId == pilgrim.Id && x.TransferType == Domain.Enums.TransferType.Arrival);
                    var waMsg = Build5hWhatsApp(user.FullName, pilgrim.ArrivalDate, pilgrim.ArrivalTime, pilgrim.ArrivalFlight, pilgrim.ArrivalAirport, t, isDeparture: false);
                    var html  = Build5hEmail(user.FullName, pilgrim.ArrivalDate, pilgrim.ArrivalTime, pilgrim.ArrivalFlight, pilgrim.ArrivalAirport, t, isDeparture: false);

                    if (!string.IsNullOrEmpty(phone)) await whatsApp.SendMessageAsync(phone, waMsg);
                    await email.SendAsync(user.Email, user.FullName, "🛬 Your flight lands in 5 hours! — Misbah ul Hoda", html);

                    pilgrim.Arrival5hReminderSentAt = now;
                    uow.Pilgrims.Update(pilgrim);
                    anyChanged = true;
                    logger.LogInformation("5h arrival reminder → {Name}", user.FullName);
                }
            }
        }

        if (anyChanged) await uow.SaveChangesAsync(ct);
    }

    private static DateTime ParseFlightDateTime(DateTime date, string? timeStr)
    {
        if (!string.IsNullOrEmpty(timeStr) && TimeSpan.TryParse(timeStr, out var ts))
            return date.Date + ts;
        // No time set: default to 12:00 so 5h reminder fires at 07:00
        return date.Date.AddHours(12);
    }

    // ── Daily 2-day reminder + admin digest ───────────────────────────────────
    public async Task RunDailyAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Running daily TravelReminderService at {Time}", DateTime.UtcNow);
        using var scope = scopeFactory.CreateScope();
        var uow      = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var email    = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        var today        = DateTime.UtcNow.Date;
        var in2Days      = today.AddDays(2);
        var in2DaysEnd   = in2Days.AddDays(1);
        var tomorrow     = today.AddDays(1);
        var tomorrowEnd  = tomorrow.AddDays(1);

        var allPilgrims = (await uow.Pilgrims.FindAsync(p => !p.IsDeleted, ct)).ToList();
        var userIds     = allPilgrims.Select(p => p.UserId).ToList();
        var users       = (await uow.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);
        var transfers   = (await uow.AirportTransfers.FindAsync(t => !t.IsDeleted, ct)).ToList();

        int departureRemindersSent = 0;
        int arrivalRemindersSent   = 0;

        // ── 2-day departure reminders ──────────────────────────────────────────
        var departingSoon = allPilgrims.Where(p =>
            p.DepartureDate >= in2Days && p.DepartureDate < in2DaysEnd &&
            p.DepartureReminderSentAt is null).ToList();

        foreach (var pilgrim in departingSoon)
        {
            if (!users.TryGetValue(pilgrim.UserId, out var user)) continue;

            var depTransfer = transfers.FirstOrDefault(t => t.PilgrimId == pilgrim.Id && t.TransferType == Domain.Enums.TransferType.Departure);

            var waMsg = BuildDepartureWhatsApp(user.FullName, pilgrim, depTransfer);
            var emailHtml = BuildDepartureEmail(user.FullName, pilgrim, depTransfer);

            var phone = user.WhatsAppNumber ?? user.PhoneNumber;
            if (!string.IsNullOrEmpty(phone))
                await whatsApp.SendMessageAsync(phone, waMsg);

            await email.SendAsync(user.Email, user.FullName, "✈️ Departure Reminder — Misbah ul Hoda Arbaeen 2026", emailHtml);

            pilgrim.DepartureReminderSentAt = DateTime.UtcNow;
            uow.Pilgrims.Update(pilgrim);
            departureRemindersSent++;
            logger.LogInformation("Departure reminder sent to {Name} ({Email})", user.FullName, user.Email);
        }

        // ── 2-day arrival reminders ────────────────────────────────────────────
        var arrivingSoon = allPilgrims.Where(p =>
            p.ArrivalDate >= in2Days && p.ArrivalDate < in2DaysEnd &&
            p.ArrivalReminderSentAt is null).ToList();

        foreach (var pilgrim in arrivingSoon)
        {
            if (!users.TryGetValue(pilgrim.UserId, out var user)) continue;

            var arrTransfer = transfers.FirstOrDefault(t => t.PilgrimId == pilgrim.Id && t.TransferType == Domain.Enums.TransferType.Arrival);

            var waMsg = BuildArrivalWhatsApp(user.FullName, pilgrim, arrTransfer);
            var emailHtml = BuildArrivalEmail(user.FullName, pilgrim, arrTransfer);

            var phone = user.WhatsAppNumber ?? user.PhoneNumber;
            if (!string.IsNullOrEmpty(phone))
                await whatsApp.SendMessageAsync(phone, waMsg);

            await email.SendAsync(user.Email, user.FullName, "🛬 Arrival Reminder — Misbah ul Hoda Arbaeen 2026", emailHtml);

            pilgrim.ArrivalReminderSentAt = DateTime.UtcNow;
            uow.Pilgrims.Update(pilgrim);
            arrivalRemindersSent++;
        }

        if (departureRemindersSent > 0 || arrivalRemindersSent > 0)
            await uow.SaveChangesAsync(ct);

        // ── Admin daily digest ─────────────────────────────────────────────────
        var arrivingTomorrow  = allPilgrims.Where(p => p.ArrivalDate >= tomorrow   && p.ArrivalDate < tomorrowEnd).ToList();
        var departingTomorrow = allPilgrims.Where(p => p.DepartureDate >= tomorrow && p.DepartureDate < tomorrowEnd).ToList();
        var arrivingIn2       = allPilgrims.Where(p => p.ArrivalDate >= in2Days    && p.ArrivalDate < in2DaysEnd).ToList();
        var departingIn2      = allPilgrims.Where(p => p.DepartureDate >= in2Days  && p.DepartureDate < in2DaysEnd).ToList();

        if (arrivingTomorrow.Any() || departingTomorrow.Any() || arrivingIn2.Any() || departingIn2.Any())
        {
            var adminUsers = (await uow.Users.FindAsync(u => (int)u.Role <= 2 && u.IsActive, ct)).ToList();
            var digestHtml = BuildAdminDigest(
                today, tomorrow, in2Days,
                arrivingTomorrow, departingTomorrow,
                arrivingIn2, departingIn2,
                users, transfers);

            foreach (var admin in adminUsers)
                await email.SendAsync(admin.Email, admin.FullName, $"📅 Daily Travel Summary — {tomorrow:dd MMM yyyy}", digestHtml);

            logger.LogInformation("Admin digest sent to {Count} admin(s). Tomorrow: {A} arrivals, {D} departures. In 2 days: {A2} arrivals, {D2} departures.",
                adminUsers.Count, arrivingTomorrow.Count, departingTomorrow.Count, arrivingIn2.Count, departingIn2.Count);
        }
    }

    // ── WhatsApp message builders ──────────────────────────────────────────────

    private static string BuildDepartureWhatsApp(string name, Domain.Entities.Pilgrim p, Domain.Entities.AirportTransfer? t)
    {
        var lines = new List<string>
        {
            $"Assalam-o-Alaikum *{name}*! 🕌",
            "",
            "*Misbah ul Hoda — Arbaeen 2026*",
            "",
            "🛫 *Departure Reminder*",
            $"Your departure is in *2 days* — *{p.DepartureDate:dddd, dd MMM yyyy}*",
        };
        if (!string.IsNullOrEmpty(p.DepartureTime))   lines.Add($"🕐 Time: *{p.DepartureTime}*");
        if (!string.IsNullOrEmpty(p.DepartureFlight)) lines.Add($"✈️ Flight: *{p.DepartureFlight}*");
        if (!string.IsNullOrEmpty(p.DepartureAirport)) lines.Add($"🏛️ Airport: *{p.DepartureAirport}*");
        lines.Add("");
        lines.Add("🎒 *Please prepare your luggage now.*");

        if (t?.DriverName is not null)
        {
            lines.Add("");
            lines.Add("🚗 *Your Pickup Driver:*");
            lines.Add($"Driver: *{t.DriverName}*");
            if (!string.IsNullOrEmpty(t.DriverPhone))  lines.Add($"Phone: *{t.DriverPhone}*");
            if (!string.IsNullOrEmpty(t.VehicleType))  lines.Add($"Vehicle: {t.VehicleType} {t.VehicleNumber}");
            if (!string.IsNullOrEmpty(t.MeetingPoint)) lines.Add($"📍 Pickup from: {t.MeetingPoint}");
            if (t.ScheduledTime.HasValue)              lines.Add($"🕐 Driver arrives at: {t.ScheduledTime.Value:HH:mm}");
        }
        else
        {
            lines.Add("");
            lines.Add("ℹ️ Driver details will be shared soon. Stay tuned.");
        }

        lines.Add("");
        lines.Add("JazakAllah Khair 🌿");
        return string.Join("\n", lines);
    }

    private static string BuildArrivalWhatsApp(string name, Domain.Entities.Pilgrim p, Domain.Entities.AirportTransfer? t)
    {
        var lines = new List<string>
        {
            $"Assalam-o-Alaikum *{name}*! 🕌",
            "",
            "*Misbah ul Hoda — Arbaeen 2026*",
            "",
            "🛬 *Arrival Reminder*",
            $"Your arrival is in *2 days* — *{p.ArrivalDate:dddd, dd MMM yyyy}*",
        };
        if (!string.IsNullOrEmpty(p.ArrivalTime))   lines.Add($"🕐 Time: *{p.ArrivalTime}*");
        if (!string.IsNullOrEmpty(p.ArrivalFlight)) lines.Add($"✈️ Flight: *{p.ArrivalFlight}*");
        if (!string.IsNullOrEmpty(p.ArrivalAirport)) lines.Add($"🏛️ Airport: *{p.ArrivalAirport}*");
        lines.Add("");
        lines.Add("🎒 *Please keep your documents handy and ensure your luggage is ready.*");

        if (t?.DriverName is not null)
        {
            lines.Add("");
            lines.Add("🚗 *Your Pickup Driver:*");
            lines.Add($"Driver: *{t.DriverName}*");
            if (!string.IsNullOrEmpty(t.DriverPhone))  lines.Add($"Phone: *{t.DriverPhone}*");
            if (!string.IsNullOrEmpty(t.VehicleType))  lines.Add($"Vehicle: {t.VehicleType} {t.VehicleNumber}");
            if (!string.IsNullOrEmpty(t.MeetingPoint)) lines.Add($"📍 Meet at: {t.MeetingPoint}");
            if (t.ScheduledTime.HasValue)              lines.Add($"🕐 Driver at: {t.ScheduledTime.Value:HH:mm}");
        }
        else
        {
            lines.Add("");
            lines.Add("ℹ️ Our team will be waiting for you at the airport. Pickup details coming soon.");
        }

        lines.Add("");
        lines.Add("JazakAllah Khair 🌿");
        return string.Join("\n", lines);
    }

    // ── Email HTML builders ────────────────────────────────────────────────────

    private static string BuildDepartureEmail(string name, Domain.Entities.Pilgrim p, Domain.Entities.AirportTransfer? t)
    {
        var driverSection = t?.DriverName is not null
            ? $"""
              <div style="background:#1e3a2f;border:1px solid #166534;border-radius:8px;padding:14px;margin-top:16px">
                <p style="color:#4ade80;font-weight:bold;margin:0 0 8px">🚗 Your Pickup Driver</p>
                <table style="font-size:13px;color:#d1fae5;width:100%">
                  <tr><td style="padding:2px 0;color:#6ee7b7">Driver:</td><td style="font-weight:bold">{t.DriverName}</td></tr>
                  {(t.DriverPhone is not null ? $"<tr><td style='color:#6ee7b7'>Phone:</td><td>{t.DriverPhone}</td></tr>" : "")}
                  {(t.VehicleType is not null ? $"<tr><td style='color:#6ee7b7'>Vehicle:</td><td>{t.VehicleType} {t.VehicleNumber}</td></tr>" : "")}
                  {(t.MeetingPoint is not null ? $"<tr><td style='color:#6ee7b7'>Pickup from:</td><td>{t.MeetingPoint}</td></tr>" : "")}
                  {(t.ScheduledTime.HasValue ? $"<tr><td style='color:#6ee7b7'>Driver arrives:</td><td>{t.ScheduledTime.Value:HH:mm} on {t.ScheduledTime.Value:dd MMM}</td></tr>" : "")}
                </table>
              </div>
              """
            : "<p style='color:#94a3b8;font-size:13px;margin-top:12px'>ℹ️ Driver details will be shared soon. Please stay tuned.</p>";

        return $"""
            <div style="font-family:sans-serif;max-width:520px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:1px solid rgba(180,83,9,0.3)">
              <h2 style="color:#d97706;margin-top:0">✈️ Departure Reminder</h2>
              <p>Assalam-o-Alaikum <strong>{name}</strong>,</p>
              <p>This is a reminder that your <strong>departure from the Arbaeen 2026 tour is in 2 days</strong>. Please prepare your luggage.</p>
              <div style="background:#1e293b;border-radius:8px;padding:16px;margin:16px 0">
                <table style="font-size:14px;color:#cbd5e1;width:100%">
                  <tr><td style="padding:4px 0;color:#94a3b8">Date:</td><td style="font-weight:bold;color:#f1f5f9">{p.DepartureDate:dddd, dd MMM yyyy}</td></tr>
                  {(p.DepartureTime is not null ? $"<tr><td style='color:#94a3b8'>Time:</td><td style='color:#f1f5f9'>{p.DepartureTime}</td></tr>" : "")}
                  {(p.DepartureFlight is not null ? $"<tr><td style='color:#94a3b8'>Flight:</td><td style='font-weight:bold;color:#f1f5f9'>{p.DepartureFlight}</td></tr>" : "")}
                  {(p.DepartureAirport is not null ? $"<tr><td style='color:#94a3b8'>Airport:</td><td style='color:#f1f5f9'>{p.DepartureAirport}</td></tr>" : "")}
                </table>
              </div>
              <div style="background:#7c2d12;border-radius:8px;padding:12px 16px;font-weight:bold;font-size:15px;text-align:center">
                🎒 Please prepare and pack your luggage now.
              </div>
              {driverSection}
              <hr style="border-color:rgba(180,83,9,0.2);margin:20px 0"/>
              <p style="color:#475569;font-size:12px">Misbah ul Hoda — Arbaeen 2026 Pilgrimage Management</p>
            </div>
            """;
    }

    private static string BuildArrivalEmail(string name, Domain.Entities.Pilgrim p, Domain.Entities.AirportTransfer? t)
    {
        var driverSection = t?.DriverName is not null
            ? $"""
              <div style="background:#1e3a2f;border:1px solid #166534;border-radius:8px;padding:14px;margin-top:16px">
                <p style="color:#4ade80;font-weight:bold;margin:0 0 8px">🚗 Your Pickup Driver</p>
                <table style="font-size:13px;color:#d1fae5;width:100%">
                  <tr><td style="padding:2px 0;color:#6ee7b7">Driver:</td><td style="font-weight:bold">{t.DriverName}</td></tr>
                  {(t.DriverPhone is not null ? $"<tr><td style='color:#6ee7b7'>Phone:</td><td>{t.DriverPhone}</td></tr>" : "")}
                  {(t.VehicleType is not null ? $"<tr><td style='color:#6ee7b7'>Vehicle:</td><td>{t.VehicleType} {t.VehicleNumber}</td></tr>" : "")}
                  {(t.MeetingPoint is not null ? $"<tr><td style='color:#6ee7b7'>Meet at:</td><td>{t.MeetingPoint}</td></tr>" : "")}
                  {(t.ScheduledTime.HasValue ? $"<tr><td style='color:#6ee7b7'>Driver at:</td><td>{t.ScheduledTime.Value:HH:mm} on {t.ScheduledTime.Value:dd MMM}</td></tr>" : "")}
                </table>
              </div>
              """
            : "<p style='color:#94a3b8;font-size:13px;margin-top:12px'>ℹ️ Our team will be waiting for you at the airport. Pickup details coming soon.</p>";

        return $"""
            <div style="font-family:sans-serif;max-width:520px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:1px solid rgba(180,83,9,0.3)">
              <h2 style="color:#d97706;margin-top:0">🛬 Arrival Reminder</h2>
              <p>Assalam-o-Alaikum <strong>{name}</strong>,</p>
              <p>Your <strong>arrival for Arbaeen 2026 is in 2 days</strong>. Please have your documents ready and ensure your luggage is packed.</p>
              <div style="background:#1e293b;border-radius:8px;padding:16px;margin:16px 0">
                <table style="font-size:14px;color:#cbd5e1;width:100%">
                  <tr><td style="padding:4px 0;color:#94a3b8">Date:</td><td style="font-weight:bold;color:#f1f5f9">{p.ArrivalDate:dddd, dd MMM yyyy}</td></tr>
                  {(p.ArrivalTime is not null ? $"<tr><td style='color:#94a3b8'>Time:</td><td style='color:#f1f5f9'>{p.ArrivalTime}</td></tr>" : "")}
                  {(p.ArrivalFlight is not null ? $"<tr><td style='color:#94a3b8'>Flight:</td><td style='font-weight:bold;color:#f1f5f9'>{p.ArrivalFlight}</td></tr>" : "")}
                  {(p.ArrivalAirport is not null ? $"<tr><td style='color:#94a3b8'>Airport:</td><td style='color:#f1f5f9'>{p.ArrivalAirport}</td></tr>" : "")}
                </table>
              </div>
              {driverSection}
              <hr style="border-color:rgba(180,83,9,0.2);margin:20px 0"/>
              <p style="color:#475569;font-size:12px">Misbah ul Hoda — Arbaeen 2026 Pilgrimage Management</p>
            </div>
            """;
    }

    private static string BuildAdminDigest(
        DateTime today, DateTime tomorrow, DateTime in2Days,
        List<Domain.Entities.Pilgrim> arrivingTomorrow,
        List<Domain.Entities.Pilgrim> departingTomorrow,
        List<Domain.Entities.Pilgrim> arrivingIn2,
        List<Domain.Entities.Pilgrim> departingIn2,
        Dictionary<Guid, Domain.Entities.User> users,
        List<Domain.Entities.AirportTransfer> transfers)
    {
        string PilgrimRows(List<Domain.Entities.Pilgrim> list, Domain.Enums.TransferType tType) =>
            string.Join("", list.Select(p =>
            {
                users.TryGetValue(p.UserId, out var u);
                var t = transfers.FirstOrDefault(x => x.PilgrimId == p.Id && x.TransferType == tType);
                var flight = tType == Domain.Enums.TransferType.Arrival ? p.ArrivalFlight : p.DepartureFlight;
                var time   = tType == Domain.Enums.TransferType.Arrival ? p.ArrivalTime   : p.DepartureTime;
                var driver = t?.DriverName ?? "<span style='color:#f87171'>⚠️ Unassigned</span>";
                return $"<tr style='border-bottom:1px solid #1e293b'>" +
                       $"<td style='padding:6px 8px;color:#f1f5f9'>{u?.FullName ?? "?"}</td>" +
                       $"<td style='padding:6px 8px;color:#94a3b8'>{u?.PhoneNumber ?? "—"}</td>" +
                       $"<td style='padding:6px 8px;color:#fbbf24'>{flight ?? "—"}</td>" +
                       $"<td style='padding:6px 8px;color:#94a3b8'>{time ?? "—"}</td>" +
                       $"<td style='padding:6px 8px;color:#4ade80'>{p.FamilyMemberCount}</td>" +
                       $"<td style='padding:6px 8px'>{driver}</td>" +
                       "</tr>";
            }));

        string Section(string title, string color, List<Domain.Entities.Pilgrim> list, Domain.Enums.TransferType t) =>
            list.Any() ? $"""
                <h3 style="color:{color};margin:20px 0 8px">{title} ({list.Count})</h3>
                <table style="width:100%;border-collapse:collapse;font-size:12px">
                  <thead><tr style="background:#1e293b;color:#64748b">
                    <th style="padding:6px 8px;text-align:left">Name</th>
                    <th style="padding:6px 8px;text-align:left">Phone</th>
                    <th style="padding:6px 8px;text-align:left">Flight</th>
                    <th style="padding:6px 8px;text-align:left">Time</th>
                    <th style="padding:6px 8px;text-align:left">Family</th>
                    <th style="padding:6px 8px;text-align:left">Driver</th>
                  </tr></thead>
                  <tbody>{PilgrimRows(list, t)}</tbody>
                </table>
                """ : "";

        return $"""
            <div style="font-family:sans-serif;max-width:700px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:1px solid rgba(180,83,9,0.3)">
              <h2 style="color:#d97706;margin-top:0">📅 Daily Travel Summary</h2>
              <p style="color:#94a3b8">Generated: {today:dddd, dd MMM yyyy} at 08:00 UTC</p>

              <div style="display:flex;gap:12px;flex-wrap:wrap;margin:16px 0">
                <div style="background:#1e293b;border-radius:8px;padding:12px 20px;text-align:center;min-width:120px">
                  <div style="font-size:24px;font-weight:bold;color:#4ade80">{arrivingTomorrow.Count}</div>
                  <div style="font-size:12px;color:#94a3b8">🛬 Arriving Tomorrow</div>
                </div>
                <div style="background:#1e293b;border-radius:8px;padding:12px 20px;text-align:center;min-width:120px">
                  <div style="font-size:24px;font-weight:bold;color:#60a5fa">{departingTomorrow.Count}</div>
                  <div style="font-size:12px;color:#94a3b8">🛫 Departing Tomorrow</div>
                </div>
                <div style="background:#1e293b;border-radius:8px;padding:12px 20px;text-align:center;min-width:120px">
                  <div style="font-size:24px;font-weight:bold;color:#a3e635">{arrivingIn2.Count}</div>
                  <div style="font-size:12px;color:#94a3b8">🛬 Arriving in 2 Days</div>
                </div>
                <div style="background:#1e293b;border-radius:8px;padding:12px 20px;text-align:center;min-width:120px">
                  <div style="font-size:24px;font-weight:bold;color:#f472b6">{departingIn2.Count}</div>
                  <div style="font-size:12px;color:#94a3b8">🛫 Departing in 2 Days</div>
                </div>
              </div>

              {Section("🛬 Arriving Tomorrow — " + tomorrow.ToString("dd MMM yyyy"), "#4ade80", arrivingTomorrow, Domain.Enums.TransferType.Arrival)}
              {Section("🛫 Departing Tomorrow — " + tomorrow.ToString("dd MMM yyyy"), "#60a5fa", departingTomorrow, Domain.Enums.TransferType.Departure)}
              {Section("🛬 Arriving in 2 Days — " + in2Days.ToString("dd MMM yyyy"), "#a3e635", arrivingIn2, Domain.Enums.TransferType.Arrival)}
              {Section("🛫 Departing in 2 Days — " + in2Days.ToString("dd MMM yyyy"), "#f472b6", departingIn2, Domain.Enums.TransferType.Departure)}

              <hr style="border-color:rgba(180,83,9,0.2);margin:20px 0"/>
              <p style="color:#475569;font-size:12px">Misbah ul Hoda — Arbaeen 2026 Pilgrimage Management</p>
            </div>
            """;
    }

    // ── 5-hour message builders ────────────────────────────────────────────────

    private static string Build5hWhatsApp(
        string name, DateTime date, string? time, string? flight, string? airport,
        Domain.Entities.AirportTransfer? t, bool isDeparture)
    {
        var emoji   = isDeparture ? "🛫" : "🛬";
        var action  = isDeparture ? "departs" : "lands";
        var label   = isDeparture ? "Departure" : "Arrival";
        var timeStr = string.IsNullOrEmpty(time) ? "soon" : time;

        var lines = new List<string>
        {
            $"Assalam-o-Alaikum *{name}*! 🕌",
            "",
            "*Misbah ul Hoda — Arbaeen 2026*",
            "",
            $"{emoji} *Your flight {action} in 5 hours!*",
            $"📅 {date:dddd, dd MMM yyyy} at *{timeStr}*",
        };
        if (!string.IsNullOrEmpty(flight))  lines.Add($"✈️ Flight: *{flight}*");
        if (!string.IsNullOrEmpty(airport)) lines.Add($"🏛️ Airport: *{airport}*");

        lines.Add("");
        lines.Add(isDeparture
            ? "🎒 *Please finalise your luggage and be ready at the pickup point.*"
            : "📋 *Keep your passport and documents ready for arrival.*");

        if (t?.DriverName is not null)
        {
            lines.Add("");
            lines.Add($"🚗 *Your {(isDeparture ? "Pickup" : "Reception")} Driver:*");
            lines.Add($"Driver: *{t.DriverName}*");
            if (!string.IsNullOrEmpty(t.DriverPhone))  lines.Add($"Phone: *{t.DriverPhone}*");
            if (!string.IsNullOrEmpty(t.VehicleType))  lines.Add($"Vehicle: {t.VehicleType} {t.VehicleNumber}");
            if (!string.IsNullOrEmpty(t.MeetingPoint)) lines.Add($"📍 {(isDeparture ? "Pickup" : "Meet")}: {t.MeetingPoint}");
            if (t.ScheduledTime.HasValue)              lines.Add($"🕐 Driver at: *{t.ScheduledTime.Value:HH:mm}*");
        }
        else
        {
            lines.Add("");
            lines.Add("ℹ️ Contact the admin team if you need assistance.");
        }

        lines.Add("");
        lines.Add("JazakAllah Khair 🌿");
        return string.Join("\n", lines);
    }

    private static string Build5hEmail(
        string name, DateTime date, string? time, string? flight, string? airport,
        Domain.Entities.AirportTransfer? t, bool isDeparture)
    {
        var emoji   = isDeparture ? "🛫" : "🛬";
        var action  = isDeparture ? "departs" : "lands";
        var label   = isDeparture ? "Departure" : "Arrival";
        var timeStr = string.IsNullOrEmpty(time) ? "—" : time;
        var color   = isDeparture ? "#3b82f6" : "#22c55e";

        var driverHtml = t?.DriverName is not null
            ? $"""
              <div style="background:#1e3a2f;border:1px solid #166534;border-radius:8px;padding:14px;margin-top:14px">
                <p style="color:#4ade80;font-weight:bold;margin:0 0 8px">🚗 Your Driver Details</p>
                <table style="font-size:13px;color:#d1fae5;width:100%">
                  <tr><td style="padding:2px 0;color:#6ee7b7">Driver:</td><td style="font-weight:bold">{t.DriverName}</td></tr>
                  {(t.DriverPhone is not null ? $"<tr><td style='color:#6ee7b7'>Phone:</td><td>{t.DriverPhone}</td></tr>" : "")}
                  {(t.VehicleType is not null ? $"<tr><td style='color:#6ee7b7'>Vehicle:</td><td>{t.VehicleType} {t.VehicleNumber}</td></tr>" : "")}
                  {(t.MeetingPoint is not null ? $"<tr><td style='color:#6ee7b7'>{(isDeparture ? "Pickup from" : "Meet at")}:</td><td>{t.MeetingPoint}</td></tr>" : "")}
                  {(t.ScheduledTime.HasValue ? $"<tr><td style='color:#6ee7b7'>Driver at:</td><td>{t.ScheduledTime.Value:HH:mm} on {t.ScheduledTime.Value:dd MMM}</td></tr>" : "")}
                </table>
              </div>
              """
            : "<p style='color:#94a3b8;font-size:13px;margin-top:10px'>ℹ️ Contact the admin team if you need assistance.</p>";

        return $"""
            <div style="font-family:sans-serif;max-width:520px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:1px solid {color}66">
              <h2 style="color:{color};margin-top:0">{emoji} Your flight {action} in 5 hours!</h2>
              <p>Assalam-o-Alaikum <strong>{name}</strong>,</p>
              <p>This is your final reminder — your <strong>{label.ToLower()} is in 5 hours</strong>. Please be ready.</p>
              <div style="background:#1e293b;border-radius:8px;padding:16px;margin:16px 0">
                <table style="font-size:14px;color:#cbd5e1;width:100%">
                  <tr><td style="padding:4px 0;color:#94a3b8">Date:</td><td style="font-weight:bold;color:#f1f5f9">{date:dddd, dd MMM yyyy}</td></tr>
                  <tr><td style="padding:4px 0;color:#94a3b8">Time:</td><td style="font-weight:bold;color:{color}">{timeStr}</td></tr>
                  {(flight  is not null ? $"<tr><td style='color:#94a3b8'>Flight:</td><td style='font-weight:bold;color:#f1f5f9'>{flight}</td></tr>"  : "")}
                  {(airport is not null ? $"<tr><td style='color:#94a3b8'>Airport:</td><td style='color:#f1f5f9'>{airport}</td></tr>" : "")}
                </table>
              </div>
              <div style="background:#7c2d12;border-radius:8px;padding:12px 16px;font-weight:bold;font-size:15px;text-align:center">
                {(isDeparture ? "🎒 Finalise your luggage. Be at the pickup point now." : "📋 Keep passport and documents ready.")}
              </div>
              {driverHtml}
              <hr style="border-color:rgba(59,130,246,0.2);margin:20px 0"/>
              <p style="color:#475569;font-size:12px">Misbah ul Hoda — Arbaeen 2026 Pilgrimage Management</p>
            </div>
            """;
    }
}
