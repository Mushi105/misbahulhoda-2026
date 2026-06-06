using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;
using Misbahuda.Infrastructure.Services;

namespace Misbahuda.API.Controllers;

public class ItineraryController(IMediator mediator, IUnitOfWork unitOfWork, ICurrentUserService currentUser, TravelReminderService reminderService)
    : BaseController(mediator)
{
    // ── Pilgrim: view my itinerary ────────────────────────────────────────────
    [HttpGet("my")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> GetMyItinerary(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var pilgrim = (await unitOfWork.Pilgrims.FindAsync(p => p.UserId == userId, ct)).FirstOrDefault();
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim profile not found."));

        var transfers = await unitOfWork.AirportTransfers.FindAsync(t => t.PilgrimId == pilgrim.Id, ct);
        var arrival   = transfers.FirstOrDefault(t => t.TransferType == TransferType.Arrival);
        var departure = transfers.FirstOrDefault(t => t.TransferType == TransferType.Departure);

        return Ok(ApiResponse<object>.Ok(new
        {
            pilgrim.Id,
            pilgrim.ArrivalDate,
            pilgrim.ArrivalFlight,
            pilgrim.ArrivalAirport,
            pilgrim.ArrivalTime,
            pilgrim.DepartureDate,
            pilgrim.DepartureFlight,
            pilgrim.DepartureAirport,
            pilgrim.DepartureTime,
            ArrivalTransfer   = arrival   is null ? null : MapTransfer(arrival),
            DepartureTransfer = departure is null ? null : MapTransfer(departure),
        }));
    }

    // ── Pilgrim: update travel details ────────────────────────────────────────
    [HttpPut("my/travel")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> UpdateTravel([FromBody] UpdateTravelRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var pilgrim = (await unitOfWork.Pilgrims.FindAsync(p => p.UserId == userId, ct)).FirstOrDefault();
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim profile not found."));

        if (req.ArrivalAirport   is not null) pilgrim.ArrivalAirport   = req.ArrivalAirport;
        if (req.DepartureAirport is not null) pilgrim.DepartureAirport = req.DepartureAirport;
        if (req.ArrivalTime      is not null) pilgrim.ArrivalTime      = req.ArrivalTime;
        if (req.DepartureTime    is not null) pilgrim.DepartureTime    = req.DepartureTime;
        if (req.ArrivalFlight    is not null) pilgrim.ArrivalFlight    = req.ArrivalFlight;
        if (req.DepartureFlight  is not null) pilgrim.DepartureFlight  = req.DepartureFlight;
        if (req.ArrivalDate.HasValue)   pilgrim.ArrivalDate   = req.ArrivalDate.Value;
        if (req.DepartureDate.HasValue) pilgrim.DepartureDate = req.DepartureDate.Value;

        unitOfWork.Pilgrims.Update(pilgrim);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Travel details updated."));
    }

    // ── Admin: today's summary ────────────────────────────────────────────────
    [HttpGet("today")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var today    = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var arriving  = (await unitOfWork.Pilgrims.FindAsync(p => p.ArrivalDate >= today && p.ArrivalDate < tomorrow, ct)).ToList();
        var departing = (await unitOfWork.Pilgrims.FindAsync(p => p.DepartureDate >= today && p.DepartureDate < tomorrow, ct)).ToList();

        var allIds = arriving.Select(p => p.Id).Concat(departing.Select(p => p.Id)).Distinct().ToList();
        var allTransfers = allIds.Any()
            ? (await unitOfWork.AirportTransfers.FindAsync(t => allIds.Contains(t.PilgrimId), ct)).ToList()
            : [];

        var allUserIds = arriving.Select(p => p.UserId).Concat(departing.Select(p => p.UserId)).Distinct().ToList();
        var users = (await unitOfWork.Users.FindAsync(u => allUserIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);

        var pendingPickups  = arriving.Count(p => !allTransfers.Any(t => t.PilgrimId == p.Id && t.TransferType == TransferType.Arrival));
        var pendingDropoffs = departing.Count(p => !allTransfers.Any(t => t.PilgrimId == p.Id && t.TransferType == TransferType.Departure));

        return Ok(ApiResponse<object>.Ok(new
        {
            Date            = today.ToString("yyyy-MM-dd"),
            ArrivingCount   = arriving.Count,
            DepartingCount  = departing.Count,
            PendingPickups  = pendingPickups,
            PendingDropoffs = pendingDropoffs,
            Arrivals        = arriving.Select(p  => BuildRow(p,  TransferType.Arrival,   users, allTransfers)),
            Departures      = departing.Select(p => BuildRow(p,  TransferType.Departure, users, allTransfers)),
        }));
    }

    // ── Admin: arrivals for a date ────────────────────────────────────────────
    [HttpGet("arrivals")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetArrivals([FromQuery] string? date, CancellationToken ct)
    {
        var day  = date is not null ? DateTime.SpecifyKind(DateTime.Parse(date).Date, DateTimeKind.Utc) : DateTime.UtcNow.Date;
        var next = day.AddDays(1);

        var pilgrims = (await unitOfWork.Pilgrims.FindAsync(p => p.ArrivalDate >= day && p.ArrivalDate < next, ct)).ToList();
        return Ok(ApiResponse<object>.Ok(await EnrichPilgrims(pilgrims, TransferType.Arrival, ct)));
    }

    // ── Admin: departures for a date ──────────────────────────────────────────
    [HttpGet("departures")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetDepartures([FromQuery] string? date, CancellationToken ct)
    {
        var day  = date is not null ? DateTime.SpecifyKind(DateTime.Parse(date).Date, DateTimeKind.Utc) : DateTime.UtcNow.Date;
        var next = day.AddDays(1);

        var pilgrims = (await unitOfWork.Pilgrims.FindAsync(p => p.DepartureDate >= day && p.DepartureDate < next, ct)).ToList();
        return Ok(ApiResponse<object>.Ok(await EnrichPilgrims(pilgrims, TransferType.Departure, ct)));
    }

    // ── Admin: assign driver ──────────────────────────────────────────────────
    [HttpPost("{pilgrimId:guid}/assign")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AssignDriver(Guid pilgrimId, [FromBody] AssignTransferRequest req, CancellationToken ct)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(pilgrimId, ct);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));

        var type = (TransferType)req.TransferType;
        var existing = (await unitOfWork.AirportTransfers.FindAsync(t => t.PilgrimId == pilgrimId && t.TransferType == type, ct)).FirstOrDefault();

        if (existing is not null)
        {
            existing.DriverName    = req.DriverName;
            existing.DriverPhone   = req.DriverPhone;
            existing.VehicleNumber = req.VehicleNumber;
            existing.VehicleType   = req.VehicleType;
            existing.MeetingPoint  = req.MeetingPoint;
            existing.ScheduledTime = req.ScheduledTime;
            existing.Notes         = req.Notes;
            existing.Status        = TransferStatus.DriverAssigned;
            unitOfWork.AirportTransfers.Update(existing);
        }
        else
        {
            await unitOfWork.AirportTransfers.AddAsync(new AirportTransfer
            {
                PilgrimId     = pilgrimId,
                TransferType  = type,
                Status        = TransferStatus.DriverAssigned,
                DriverName    = req.DriverName,
                DriverPhone   = req.DriverPhone,
                VehicleNumber = req.VehicleNumber,
                VehicleType   = req.VehicleType,
                MeetingPoint  = req.MeetingPoint,
                ScheduledTime = req.ScheduledTime,
                Notes         = req.Notes,
            }, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Driver assigned successfully."));
    }

    // ── Admin: update transfer status ─────────────────────────────────────────
    [HttpPatch("transfers/{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        var transfer = await unitOfWork.AirportTransfers.GetByIdAsync(id, ct);
        if (transfer is null) return NotFound(ApiResponse<object>.Fail("Transfer not found."));

        transfer.Status = (TransferStatus)req.Status;
        if ((TransferStatus)req.Status == TransferStatus.Completed)
            transfer.CompletedAt = DateTime.UtcNow;

        unitOfWork.AirportTransfers.Update(transfer);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Status updated."));
    }

    // ── Admin: upcoming arrivals/departures (next N days) ─────────────────────
    [HttpGet("upcoming")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var end   = today.AddDays(days);

        var pilgrims = (await unitOfWork.Pilgrims.FindAsync(
            p => (p.ArrivalDate >= today && p.ArrivalDate < end) ||
                 (p.DepartureDate >= today && p.DepartureDate < end), ct)).ToList();

        if (!pilgrims.Any()) return Ok(ApiResponse<object>.Ok(new { Days = days, Dates = Array.Empty<object>() }));

        var ids      = pilgrims.Select(p => p.Id).ToList();
        var userIds  = pilgrims.Select(p => p.UserId).ToList();
        var transfers = (await unitOfWork.AirportTransfers.FindAsync(t => ids.Contains(t.PilgrimId), ct)).ToList();
        var users     = (await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);

        // Group by date
        var allDates = new SortedDictionary<DateTime, object>();
        for (var d = today; d < end; d = d.AddDays(1))
        {
            var arriving  = pilgrims.Where(p => p.ArrivalDate.Date  == d).ToList();
            var departing = pilgrims.Where(p => p.DepartureDate.Date == d).ToList();
            if (!arriving.Any() && !departing.Any()) continue;

            allDates[d] = new
            {
                Date           = d.ToString("yyyy-MM-dd"),
                DayLabel       = d.ToString("dddd, dd MMM yyyy"),
                ArrivingCount  = arriving.Count,
                DepartingCount = departing.Count,
                Arrivals       = arriving.Select(p  => BuildRow(p, TransferType.Arrival,   users, transfers)),
                Departures     = departing.Select(p => BuildRow(p, TransferType.Departure, users, transfers)),
            };
        }

        return Ok(ApiResponse<object>.Ok(new { Days = days, Dates = allDates.Values }));
    }

    // ── Admin: manual trigger reminders ───────────────────────────────────────
    [HttpPost("send-reminders")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SendReminders(CancellationToken ct)
    {
        _ = Task.Run(() => reminderService.RunAsync(ct), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Reminders are being sent in the background."));
    }

    // ── Admin: remove transfer assignment ─────────────────────────────────────
    [HttpDelete("transfers/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteTransfer(Guid id, CancellationToken ct)
    {
        var transfer = await unitOfWork.AirportTransfers.GetByIdAsync(id, ct);
        if (transfer is null) return NotFound(ApiResponse<object>.Fail("Transfer not found."));

        unitOfWork.AirportTransfers.Delete(transfer);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Removed."));
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private async Task<IEnumerable<object>> EnrichPilgrims(List<Pilgrim> pilgrims, TransferType type, CancellationToken ct)
    {
        if (!pilgrims.Any()) return [];
        var ids       = pilgrims.Select(p => p.Id).ToList();
        var userIds   = pilgrims.Select(p => p.UserId).ToList();
        var transfers = (await unitOfWork.AirportTransfers.FindAsync(t => ids.Contains(t.PilgrimId) && t.TransferType == type, ct)).ToList();
        var users     = (await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);
        return pilgrims.Select(p => BuildRow(p, type, users, transfers));
    }

    private static object BuildRow(Pilgrim p, TransferType type, Dictionary<Guid, User> users, List<AirportTransfer> transfers)
    {
        var isArrival = type == TransferType.Arrival;
        var transfer  = transfers.FirstOrDefault(t => t.PilgrimId == p.Id && t.TransferType == type);
        users.TryGetValue(p.UserId, out var user);
        return new
        {
            PilgrimId      = p.Id,
            FullName       = user?.FullName ?? "—",
            Phone          = user?.PhoneNumber,
            WhatsApp       = user?.WhatsAppNumber,
            Country        = p.Country,
            FamilyCount    = p.FamilyMemberCount,
            FlightNumber   = isArrival ? p.ArrivalFlight    : p.DepartureFlight,
            Airport        = isArrival ? p.ArrivalAirport   : p.DepartureAirport,
            Date           = isArrival ? p.ArrivalDate       : p.DepartureDate,
            Time           = isArrival ? p.ArrivalTime       : p.DepartureTime,
            TransferId     = transfer?.Id,
            TransferStatus = transfer?.Status.ToString() ?? "Unassigned",
            Transfer       = transfer is null ? null : MapTransfer(transfer),
        };
    }

    private static object MapTransfer(AirportTransfer t) => new
    {
        t.Id, t.TransferType, t.Status,
        t.DriverName, t.DriverPhone,
        t.VehicleNumber, t.VehicleType,
        t.MeetingPoint, t.ScheduledTime,
        t.Notes, t.CompletedAt,
    };
}

public record UpdateTravelRequest(
    string? ArrivalFlight, string? DepartureFlight,
    string? ArrivalAirport, string? DepartureAirport,
    string? ArrivalTime, string? DepartureTime,
    DateTime? ArrivalDate, DateTime? DepartureDate);

public record AssignTransferRequest(
    int     TransferType,
    string? DriverName, string? DriverPhone,
    string? VehicleNumber, string? VehicleType,
    string? MeetingPoint, DateTime? ScheduledTime, string? Notes);

public record UpdateStatusRequest(int Status);
