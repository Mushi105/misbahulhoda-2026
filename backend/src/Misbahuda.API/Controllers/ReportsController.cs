using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize(Roles = "SuperAdmin,Admin")]
public class ReportsController(IMediator mediator, IUnitOfWork unitOfWork) : BaseController(mediator)
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var users      = await unitOfWork.Users.GetAllAsync(cancellationToken);
        var pilgrims   = await unitOfWork.Pilgrims.GetAllAsync(cancellationToken);
        var volunteers = await unitOfWork.Volunteers.GetAllAsync(cancellationToken);
        var rooms      = await unitOfWork.Rooms.GetAllAsync(cancellationToken);
        var buses      = await unitOfWork.Buses.GetAllAsync(cancellationToken);
        var docs       = await unitOfWork.TourDocuments.FindAsync(d => d.IsActive, cancellationToken);

        var pilgrimList = pilgrims.ToList();
        var userList    = users.ToList();
        var volList     = volunteers.ToList();
        var roomList    = rooms.ToList();
        var busList     = buses.ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            GeneratedAt = DateTime.UtcNow,

            Users = new
            {
                Total      = userList.Count,
                Active     = userList.Count(u => u.IsActive),
                Inactive   = userList.Count(u => !u.IsActive),
                ByRole = new
                {
                    SuperAdmin       = userList.Count(u => u.Role == UserRole.SuperAdmin),
                    Admin            = userList.Count(u => u.Role == UserRole.Admin),
                    Pilgrim          = userList.Count(u => u.Role == UserRole.Pilgrim),
                    Volunteer        = userList.Count(u => u.Role == UserRole.Volunteer),
                    VolunteerManager = userList.Count(u => u.Role == UserRole.VolunteerManager),
                    Driver           = userList.Count(u => u.Role == UserRole.Driver),
                }
            },

            Pilgrims = new
            {
                Total       = pilgrimList.Count,
                Pending     = pilgrimList.Count(p => p.Status == ApplicationStatus.Pending),
                UnderReview = pilgrimList.Count(p => p.Status == ApplicationStatus.UnderReview),
                Approved    = pilgrimList.Count(p => p.Status == ApplicationStatus.Approved),
                Rejected    = pilgrimList.Count(p => p.Status == ApplicationStatus.Rejected),
                Cancelled   = pilgrimList.Count(p => p.Status == ApplicationStatus.Cancelled),
                WithRoom    = pilgrimList.Count(p => p.RoomId.HasValue),
                WithoutRoom = pilgrimList.Count(p => !p.RoomId.HasValue),
                WithBus     = pilgrimList.Count(p => p.BusId.HasValue),
                WithoutBus  = pilgrimList.Count(p => !p.BusId.HasValue),
                ByCountry   = pilgrimList
                    .GroupBy(p => p.Country ?? "Unknown")
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new { Country = g.Key, Count = g.Count() }),
            },

            Volunteers = new
            {
                Total            = volList.Count,
                Available        = volList.Count(v => v.Status == VolunteerStatus.Available),
                Busy             = volList.Count(v => v.Status == VolunteerStatus.Busy),
                Offline          = volList.Count(v => v.Status == VolunteerStatus.Offline),
                EmergencyAssigned = volList.Count(v => v.Status == VolunteerStatus.EmergencyAssigned),
                CheckedIn        = volList.Count(v => v.IsCheckedIn),
            },

            Accommodation = new
            {
                TotalRooms     = roomList.Count,
                AvailableRooms = roomList.Count(r => r.IsAvailable),
                OccupiedRooms  = roomList.Count(r => !r.IsAvailable),
                TotalBeds      = roomList.Sum(r => r.BedCapacity),
                OccupiedBeds   = roomList.Sum(r => r.OccupiedBeds),
                OccupancyRate  = roomList.Any()
                    ? Math.Round((double)roomList.Count(r => !r.IsAvailable) / roomList.Count * 100, 1)
                    : 0,
            },

            Transport = new
            {
                TotalBuses       = busList.Count,
                ActiveBuses      = busList.Count(b => b.IsActive),
                TotalCapacity    = busList.Sum(b => b.Capacity),
                TotalPassengers  = busList.Sum(b => b.CurrentPassengers),
            },

            Documents = new
            {
                Total = docs.Count(),
            }
        }));
    }

    [HttpGet("pilgrims/export")]
    public async Task<IActionResult> ExportPilgrims(CancellationToken cancellationToken)
    {
        var pilgrims = await unitOfWork.Pilgrims.GetAllAsync(cancellationToken);
        var pilgrimList = pilgrims.OrderByDescending(p => p.CreatedAt).ToList();

        var userIds = pilgrimList.Select(p => p.UserId).Distinct().ToList();
        var users = userIds.Any()
            ? await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken)
            : [];
        var uMap = users.ToDictionary(u => u.Id);

        // Room lookup
        var roomIds = pilgrimList.Where(p => p.RoomId.HasValue).Select(p => p.RoomId!.Value).ToList();
        var roomMap = new Dictionary<Guid, string>();
        var hotelMap = new Dictionary<Guid, string>();

        if (roomIds.Any())
        {
            var roomEntities = await unitOfWork.Rooms.FindAsync(r => roomIds.Contains(r.Id), cancellationToken);
            foreach (var r in roomEntities) roomMap[r.Id] = r.RoomNumber;
        }

        // Bus lookup
        var busIds = pilgrimList.Where(p => p.BusId.HasValue).Select(p => p.BusId!.Value).ToList();
        var busMap = new Dictionary<Guid, string>();
        if (busIds.Any())
        {
            var busEntities = await unitOfWork.Buses.FindAsync(b => busIds.Contains(b.Id), cancellationToken);
            foreach (var b in busEntities) busMap[b.Id] = b.BusNumber;
        }

        var rows = pilgrimList.Select((p, i) =>
        {
            uMap.TryGetValue(p.UserId, out var u);
            return new
            {
                No          = i + 1,
                FullName    = u?.FullName ?? "Unknown",
                Email       = u?.Email ?? "",
                Phone       = u?.PhoneNumber ?? "",
                WhatsApp    = u?.WhatsAppNumber ?? "",
                City        = u?.City ?? "",
                Postcode    = u?.Postcode ?? "",
                Country     = p.Country ?? "",
                Passport    = p.PassportNumber ?? "",
                Visa        = p.VisaNumber ?? "",
                Status      = p.Status.ToString(),
                Family      = p.FamilyMemberCount,
                Arrival     = p.ArrivalDate != default ? p.ArrivalDate.ToString("dd/MM/yyyy") : "",
                Departure   = p.DepartureDate != default ? p.DepartureDate.ToString("dd/MM/yyyy") : "",
                Room        = p.RoomId.HasValue && roomMap.TryGetValue(p.RoomId.Value, out var rn) ? rn : "Not Assigned",
                Bus         = p.BusId.HasValue && busMap.TryGetValue(p.BusId.Value, out var bn) ? bn : "Not Assigned",
                Registered  = p.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            };
        });

        return Ok(ApiResponse<object>.Ok(rows));
    }

    [HttpGet("volunteers/export")]
    public async Task<IActionResult> ExportVolunteers(CancellationToken cancellationToken)
    {
        var volunteers = await unitOfWork.Volunteers.GetAllAsync(cancellationToken);
        var volList = volunteers.OrderBy(v => v.CreatedAt).ToList();

        var userIds = volList.Select(v => v.UserId).Distinct().ToList();
        var users = userIds.Any()
            ? await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken)
            : [];
        var uMap = users.ToDictionary(u => u.Id);

        var rows = volList.Select((v, i) =>
        {
            uMap.TryGetValue(v.UserId, out var u);
            return new
            {
                No              = i + 1,
                FullName        = u?.FullName ?? "Unknown",
                Email           = u?.Email ?? "",
                Phone           = u?.PhoneNumber ?? "",
                WhatsApp        = u?.WhatsAppNumber ?? "",
                Status          = v.Status.ToString(),
                IsCheckedIn     = v.IsCheckedIn ? "Yes" : "No",
                Area            = v.AssignedArea ?? "Unassigned",
                TasksCompleted  = v.TotalTasksCompleted,
                LastCheckIn     = v.LastCheckIn.HasValue ? v.LastCheckIn.Value.ToString("dd/MM/yyyy HH:mm") : "",
                Registered      = v.CreatedAt.ToString("dd/MM/yyyy"),
            };
        });

        return Ok(ApiResponse<object>.Ok(rows));
    }

    [HttpGet("scholars/summary")]
    public async Task<IActionResult> GetScholarsSummary(CancellationToken cancellationToken)
    {
        var scholars = (await unitOfWork.Scholars.GetAllAsync(cancellationToken)).ToList();

        var allPrograms = new[] { "Majlis", "Noha", "Hadith", "Marsiya", "Dua", "Ziarat", "Lecture", "Manqabat" };

        var byProgram = allPrograms.Select(p => new
        {
            Program = p,
            Count   = scholars.Count(s => (s.Specialty ?? "").Contains(p))
        }).Where(x => x.Count > 0).ToList();

        var byLanguage = scholars
            .GroupBy(s => s.Language ?? "Unknown")
            .Select(g => new { Language = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();

        var byLocation = scholars
            .GroupBy(s => s.Location ?? "Unknown")
            .Select(g => new { Location = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            total     = scholars.Count,
            scholars  = scholars.Count(s => !s.IsReciter),
            reciters  = scholars.Count(s => s.IsReciter),
            active    = scholars.Count(s => s.IsActive),
            inactive  = scholars.Count(s => !s.IsActive),
            byProgram,
            byLanguage,
            byLocation,
        }));
    }

    [HttpGet("scholars/export")]
    public async Task<IActionResult> ExportScholars(CancellationToken cancellationToken)
    {
        var scholars = (await unitOfWork.Scholars.GetAllAsync(cancellationToken))
            .OrderBy(s => s.IsReciter).ThenBy(s => s.SortOrder).ThenBy(s => s.FullName)
            .ToList();

        var rows = scholars.Select((s, i) => new
        {
            No           = i + 1,
            Type         = s.IsReciter ? "Reciter" : "Scholar",
            FullName     = s.FullName,
            Title        = s.Title ?? "",
            ProgramTypes = s.Specialty ?? "",
            Language     = s.Language ?? "",
            Location     = s.Location ?? "",
            Tags         = s.Tags ?? "",
            Website      = s.Website ?? "",
            YouTube      = s.YoutubeUrl ?? "",
            Active       = s.IsActive ? "Yes" : "No",
        });

        return Ok(ApiResponse<object>.Ok(rows));
    }
}
