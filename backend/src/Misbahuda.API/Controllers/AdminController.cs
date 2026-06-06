using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Pilgrim;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;
using Misbahuda.Infrastructure.Services;

namespace Misbahuda.API.Controllers;

[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminController(IMediator mediator, IUnitOfWork unitOfWork, IWhatsAppService whatsApp, IEmailService email) : BaseController(mediator)
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var pilgrims = await unitOfWork.Pilgrims.GetAllAsync(cancellationToken);
        var volunteers = await unitOfWork.Volunteers.GetAllAsync(cancellationToken);
        var rooms = await unitOfWork.Rooms.GetAllAsync(cancellationToken);
        var karwans = await unitOfWork.Karwans.GetAllAsync(cancellationToken);
        var buses = await unitOfWork.Buses.GetAllAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            Pilgrims = new
            {
                Total = pilgrims.Count(),
                Pending = pilgrims.Count(p => p.Status == ApplicationStatus.Pending),
                Approved = pilgrims.Count(p => p.Status == ApplicationStatus.Approved),
                Rejected = pilgrims.Count(p => p.Status == ApplicationStatus.Rejected)
            },
            Volunteers = new
            {
                Total = volunteers.Count(),
                Available = volunteers.Count(v => v.Status == VolunteerStatus.Available),
                Busy = volunteers.Count(v => v.Status == VolunteerStatus.Busy),
                Offline = volunteers.Count(v => v.Status == VolunteerStatus.Offline)
            },
            Accommodation = new
            {
                TotalRooms = rooms.Count(),
                AvailableRooms = rooms.Count(r => r.IsAvailable),
                OccupiedRooms = rooms.Count(r => !r.IsAvailable),
                OccupancyRate = rooms.Any()
                    ? Math.Round((double)rooms.Count(r => !r.IsAvailable) / rooms.Count() * 100, 1)
                    : 0
            },
            Transport = new
            {
                TotalKarwans = karwans.Count(),
                ActiveKarwans = karwans.Count(k => k.IsActive),
                TotalBuses = buses.Count(),
                ActiveBuses = buses.Count(b => b.IsActive)
            }
        }));
    }

    [HttpGet("pilgrims")]
    public async Task<IActionResult> GetPilgrims([FromQuery] PaginationQuery query, [FromQuery] ApplicationStatus? status, CancellationToken cancellationToken)
    {
        var pilgrims = status.HasValue
            ? await unitOfWork.Pilgrims.FindAsync(p => p.Status == status, cancellationToken)
            : await unitOfWork.Pilgrims.GetAllAsync(cancellationToken);

        if (!string.IsNullOrEmpty(query.Search))
        {
            var search = query.Search.ToLower();
            var userIds = pilgrims.Select(p => p.UserId).ToList();
            var users = await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken);
            var matchingUserIds = users.Where(u => u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search)).Select(u => u.Id).ToHashSet();
            pilgrims = pilgrims.Where(p => matchingUserIds.Contains(p.UserId) || (p.PassportNumber?.ToLower().Contains(search) ?? false));
        }

        var total = pilgrims.Count();
        var paged = pilgrims.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        var userIds2 = paged.Select(p => p.UserId).ToList();
        var enrichUsers = await unitOfWork.Users.FindAsync(u => userIds2.Contains(u.Id), cancellationToken);
        var userMap = enrichUsers.ToDictionary(u => u.Id);

        // Load room details (room → floor → building → hotel)
        var roomIds = paged.Where(p => p.RoomId.HasValue).Select(p => p.RoomId!.Value).ToList();
        var rooms = roomIds.Any()
            ? await unitOfWork.Rooms.FindAsync(r => roomIds.Contains(r.Id), cancellationToken)
            : [];
        var roomMap = rooms.ToDictionary(r => r.Id);

        var floorIds = rooms.Select(r => r.FloorId).Distinct().ToList();
        var floors = floorIds.Any()
            ? await unitOfWork.Floors.FindAsync(f => floorIds.Contains(f.Id), cancellationToken)
            : [];
        var floorMap = floors.ToDictionary(f => f.Id);

        var buildingIds = floors.Select(f => f.BuildingId).Distinct().ToList();
        var buildings = buildingIds.Any()
            ? await unitOfWork.Buildings.FindAsync(b => buildingIds.Contains(b.Id), cancellationToken)
            : [];
        var buildingMap = buildings.ToDictionary(b => b.Id);

        var hotelIds = buildings.Select(b => b.HotelId).Distinct().ToList();
        var hotels = hotelIds.Any()
            ? await unitOfWork.Hotels.FindAsync(h => hotelIds.Contains(h.Id), cancellationToken)
            : [];
        var hotelMap = hotels.ToDictionary(h => h.Id);

        // Load bus details
        var busIds = paged.Where(p => p.BusId.HasValue).Select(p => p.BusId!.Value).ToList();
        var buses = busIds.Any()
            ? await unitOfWork.Buses.FindAsync(b => busIds.Contains(b.Id), cancellationToken)
            : [];
        var busMap = buses.ToDictionary(b => b.Id);

        var items = paged.Select(p =>
        {
            userMap.TryGetValue(p.UserId, out var u);

            string? roomNumber = null;
            string? hotelName = null;
            string? floorLabel = null;
            if (p.RoomId.HasValue && roomMap.TryGetValue(p.RoomId.Value, out var room))
            {
                roomNumber = room.RoomNumber;
                if (floorMap.TryGetValue(room.FloorId, out var floor))
                {
                    floorLabel = floor.Label ?? $"Floor {floor.FloorNumber}";
                    if (buildingMap.TryGetValue(floor.BuildingId, out var building)
                        && hotelMap.TryGetValue(building.HotelId, out var hotel))
                        hotelName = hotel.Name;
                }
            }

            string? busNumber = null;
            if (p.BusId.HasValue && busMap.TryGetValue(p.BusId.Value, out var bus))
                busNumber = bus.BusNumber;

            return new
            {
                p.Id, p.PassportNumber, p.VisaNumber, p.Country, p.PackageType,
                p.ArrivalDate, p.DepartureDate, p.FamilyMemberCount,
                Status = p.Status.ToString(), p.RejectionReason, p.RoomId, p.BusId,
                p.CreatedAt,
                FullName = u?.FullName ?? "Unknown",
                Email = u?.Email ?? "",
                Phone = u?.PhoneNumber ?? "",
                WhatsApp = u?.WhatsAppNumber ?? "",
                RoomNumber = roomNumber,
                HotelName = hotelName,
                FloorLabel = floorLabel,
                BusNumber = busNumber,
            };
        });

        return Ok(ApiResponse<object>.Ok(new { Items = items, TotalCount = total, query.Page, query.PageSize }));
    }

    [HttpGet("pilgrims/{id}")]
    public async Task<IActionResult> GetPilgrimDetail(Guid id, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(id, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));

        var user = await unitOfWork.Users.GetByIdAsync(pilgrim.UserId, cancellationToken);
        var familyMembers = await unitOfWork.FamilyMembers.FindAsync(f => f.PilgrimId == pilgrim.Id, cancellationToken);
        var roomHistory = await unitOfWork.PilgrimRoomHistories.FindAsync(h => h.PilgrimId == pilgrim.Id, cancellationToken);

        object? currentRoom = null;
        if (pilgrim.RoomId.HasValue)
        {
            var room = await unitOfWork.Rooms.GetByIdAsync(pilgrim.RoomId.Value, cancellationToken);
            if (room is not null)
            {
                var floor = await unitOfWork.Floors.GetByIdAsync(room.FloorId, cancellationToken);
                string hotelName = "", hotelCity = "";
                if (floor is not null)
                {
                    var building = await unitOfWork.Buildings.GetByIdAsync(floor.BuildingId, cancellationToken);
                    if (building is not null)
                    {
                        var hotel = await unitOfWork.Hotels.GetByIdAsync(building.HotelId, cancellationToken);
                        if (hotel is not null) { hotelName = hotel.Name; hotelCity = hotel.City; }
                    }
                }
                currentRoom = new { room.RoomNumber, FloorLabel = floor?.Label ?? $"Floor {floor?.FloorNumber}", HotelName = hotelName, HotelCity = hotelCity };
            }
        }

        object? currentBus = null;
        if (pilgrim.BusId.HasValue)
        {
            var bus = await unitOfWork.Buses.GetByIdAsync(pilgrim.BusId.Value, cancellationToken);
            if (bus is not null) currentBus = new { bus.BusNumber, bus.PlateNumber, bus.DriverName, bus.DriverPhone };
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            pilgrim.Id, pilgrim.PassportNumber, pilgrim.VisaNumber, pilgrim.Country, Status = pilgrim.Status.ToString(),
            pilgrim.ArrivalDate, pilgrim.DepartureDate, pilgrim.ArrivalFlight, pilgrim.DepartureFlight,
            pilgrim.FamilyMemberCount, pilgrim.RejectionReason, pilgrim.CreatedAt,
            pilgrim.EmergencyContactName, pilgrim.EmergencyContactPhone,
            User = user is null ? null : new { user.FullName, user.Email, user.PhoneNumber, user.WhatsAppNumber },
            CurrentRoom = currentRoom,
            CurrentBus = currentBus,
            FamilyMembers = familyMembers.OrderBy(f => f.CreatedAt).Select(f => new
            {
                f.Id, f.FullName, f.Gender, f.Relationship, f.DateOfBirth,
                f.PassportNumber, f.Nationality, f.RequiresWheelchair, f.IsMinor, f.SpecialNotes
            }),
            HotelTrail = roomHistory.OrderBy(h => h.CheckedInAt).Select(h => new
            {
                h.HotelName, h.HotelCity, h.RoomNumber, h.FloorLabel, h.CheckedInAt, h.CheckedOutAt, h.CheckedOutBy,
                IsCurrentStay = h.CheckedOutAt == null
            })
        }));
    }

    [HttpPatch("pilgrims/{id}/status")]
    public async Task<IActionResult> UpdatePilgrimStatus(Guid id, [FromBody] PilgrimStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(id, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));

        pilgrim.Status = request.Status;
        pilgrim.RejectionReason = request.RejectionReason;
        unitOfWork.Pilgrims.Update(pilgrim);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, $"Pilgrim status updated to {request.Status}."));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var all = await unitOfWork.Users.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            all = all.Where(u =>
                u.FullName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s) ||
                (u.PhoneNumber ?? "").Contains(s));
        }

        var total = all.Count();
        var paged = all.OrderBy(u => u.FullName)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(u => new { u.Id, u.FullName, u.Email, u.PhoneNumber, u.WhatsAppNumber, Role = u.Role.ToString(), u.IsActive, u.CreatedAt });

        return Ok(ApiResponse<object>.Ok(new { Items = paged, TotalCount = total }));
    }

    [HttpPatch("users/{id}/toggle-active")]
    public async Task<IActionResult> ToggleUserActive(Guid id, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null) return NotFound(ApiResponse<object>.Fail("User not found."));

        user.IsActive = !user.IsActive;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { user.IsActive }, user.IsActive ? "User activated." : "User deactivated."));
    }

    [HttpPost("users/create")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new Misbahuda.Application.Features.Auth.Commands.RegisterCommand(
            request.FullName, request.Email, request.PhoneNumber,
            request.Password, request.WhatsAppNumber,
            null, null, null, null, null, null, null, null, null, null, null,
            request.Role);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("users/{id}/change-role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null) return NotFound(ApiResponse<object>.Fail("User not found."));

        user.Role = request.Role;
        unitOfWork.Users.Update(user);

        // Auto-create volunteer profile when role is set to Volunteer or VolunteerManager
        if (request.Role == UserRole.Volunteer || request.Role == UserRole.VolunteerManager)
        {
            var exists = await unitOfWork.Volunteers.ExistsAsync(v => v.UserId == id, cancellationToken);
            if (!exists)
            {
                var volunteer = new Volunteer
                {
                    UserId = id,
                    Status = VolunteerStatus.Available,
                    EmergencyPhone = user.PhoneNumber,
                };
                await unitOfWork.Volunteers.AddAsync(volunteer, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { Role = user.Role.ToString() }, $"Role changed to {request.Role}."));
    }

    // ─── ALLOCATE ROOM ─────────────────────────────────────────────────────────
    [HttpPost("pilgrims/{pilgrimId}/allocate-room")]
    public async Task<IActionResult> AllocateRoom(Guid pilgrimId, [FromBody] AllocateRoomRequest request, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(pilgrimId, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));
        var room = await unitOfWork.Rooms.GetByIdAsync(request.RoomId, cancellationToken);
        if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found."));
        if (room.OccupiedBeds >= room.BedCapacity)
            return BadRequest(ApiResponse<object>.Fail("Room is full."));

        if (pilgrim.RoomId.HasValue)
        {
            var oldRoom = await unitOfWork.Rooms.GetByIdAsync(pilgrim.RoomId.Value, cancellationToken);
            if (oldRoom is not null) { oldRoom.OccupiedBeds = Math.Max(0, oldRoom.OccupiedBeds - 1); unitOfWork.Rooms.Update(oldRoom); }
        }

        pilgrim.RoomId = request.RoomId;
        room.OccupiedBeds++;
        unitOfWork.Pilgrims.Update(pilgrim);
        unitOfWork.Rooms.Update(room);

        var user = await unitOfWork.Users.GetByIdAsync(pilgrim.UserId, cancellationToken);
        var notif = new Notification
        {
            UserId = pilgrim.UserId, Title = "Room Allocated",
            Message = $"You have been assigned to Room {room.RoomNumber}. Please report to the hotel reception.",
            Type = NotificationType.Push, Event = NotificationEvent.RoomAllocation
        };
        await unitOfWork.Notifications.AddAsync(notif, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(user?.WhatsAppNumber))
            _ = whatsApp.SendMessageAsync(user.WhatsAppNumber, $"Assalamu Alaikum {user.FullName},\n\nYour room has been allocated: Room {room.RoomNumber}.\nPlease report to the hotel reception.\n\n- Misbahuda Team 🕌");

        return Ok(ApiResponse<object>.Ok(new { room.RoomNumber }, $"Room {room.RoomNumber} allocated."));
    }

    // ─── ALLOCATE BUS ──────────────────────────────────────────────────────────
    [HttpPost("pilgrims/{pilgrimId}/allocate-bus")]
    public async Task<IActionResult> AllocateBus(Guid pilgrimId, [FromBody] AllocateBusRequest request, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(pilgrimId, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));
        var bus = await unitOfWork.Buses.GetByIdAsync(request.BusId, cancellationToken);
        if (bus is null) return NotFound(ApiResponse<object>.Fail("Bus not found."));
        if (bus.CurrentPassengers >= bus.Capacity)
            return BadRequest(ApiResponse<object>.Fail("Bus is full."));

        if (pilgrim.BusId.HasValue)
        {
            var oldBus = await unitOfWork.Buses.GetByIdAsync(pilgrim.BusId.Value, cancellationToken);
            if (oldBus is not null) { oldBus.CurrentPassengers = Math.Max(0, oldBus.CurrentPassengers - 1); unitOfWork.Buses.Update(oldBus); }
        }

        pilgrim.BusId = request.BusId;
        bus.CurrentPassengers++;
        unitOfWork.Pilgrims.Update(pilgrim);
        unitOfWork.Buses.Update(bus);

        var user = await unitOfWork.Users.GetByIdAsync(pilgrim.UserId, cancellationToken);
        var notif = new Notification
        {
            UserId = pilgrim.UserId, Title = "Bus Allocated",
            Message = $"You have been assigned to Bus {bus.BusNumber} ({bus.PlateNumber}). Driver: {bus.DriverName ?? "TBA"}, Contact: {bus.DriverPhone ?? "TBA"}.",
            Type = NotificationType.Push, Event = NotificationEvent.BusDeparture
        };
        await unitOfWork.Notifications.AddAsync(notif, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(user?.WhatsAppNumber))
            _ = whatsApp.SendMessageAsync(user.WhatsAppNumber, $"Assalamu Alaikum {user.FullName},\n\nYou have been assigned to Bus {bus.BusNumber}.\nDriver: {bus.DriverName ?? "TBA"} | {bus.DriverPhone ?? ""}\n\n- Misbahuda Team 🕌");

        return Ok(ApiResponse<object>.Ok(new { bus.BusNumber }, $"Bus {bus.BusNumber} allocated."));
    }

    // ─── PILGRIM STATUS WORKFLOW ──────────────────────────────────────────────

    [HttpPatch("pilgrims/{id}/under-review")]
    public async Task<IActionResult> MarkUnderReview(Guid id, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(id, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        if (pilgrim.Status != ApplicationStatus.Pending)
            return BadRequest(ApiResponse<object>.Fail("Only Pending applications can be moved to Under Review."));

        pilgrim.Status = ApplicationStatus.UnderReview;
        unitOfWork.Pilgrims.Update(pilgrim);

        await unitOfWork.Notifications.AddAsync(new Notification
        {
            UserId = pilgrim.UserId, Title = "Application Under Review 🔍",
            Message = "Your Arbaeen 2026 application is now being reviewed by our team. You will be notified of the decision soon.",
            Type = NotificationType.Push, Event = NotificationEvent.Approval
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await unitOfWork.Users.GetByIdAsync(pilgrim.UserId, cancellationToken);
        if (user is not null)
        {
            _ = Task.Run(async () =>
            {
                if (!string.IsNullOrEmpty(user.WhatsAppNumber))
                    await whatsApp.SendMessageAsync(user.WhatsAppNumber,
                        $"Assalamu Alaikum {user.FullName},\n\n🔍 Your Arbaeen 2026 application is now *Under Review*.\n\nOur team is carefully reviewing your application. You will receive a decision notification shortly.\n\n- Misbah ul Hoda Team 🕌");

                await email.SendAsync(user.Email, user.FullName, "Application Under Review — Misbah ul Hoda", $"""
                    <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;background:#010f05;color:#f1f5f9;border-radius:12px;overflow:hidden;">
                      <div style="background:linear-gradient(135deg,#065f46,#047857);padding:32px 24px;text-align:center;">
                        <h1 style="color:#fff;margin:0;font-size:22px;">🔍 Application Under Review</h1>
                        <p style="color:#a7f3d0;margin:8px 0 0;">Arbaeen 2026 — Misbah ul Hoda</p>
                      </div>
                      <div style="padding:28px 24px;">
                        <p style="color:#d1fae5;font-size:16px;">Assalamu Alaikum <strong>{user.FullName}</strong>,</p>
                        <p style="color:#94a3b8;">Your Arbaeen 2026 pilgrimage application is now being carefully reviewed by our team.</p>
                        <div style="background:rgba(16,185,129,0.1);border:1px solid rgba(16,185,129,0.3);border-radius:8px;padding:16px;margin:20px 0;text-align:center;">
                          <p style="color:#6ee7b7;font-size:18px;margin:0;font-weight:bold;">Status: Under Review</p>
                        </div>
                        <p style="color:#94a3b8;">You will receive an email and WhatsApp notification once a decision is made. This usually takes 1–2 business days.</p>
                        <p style="color:#6b7280;font-size:13px;margin-top:24px;">— Misbah ul Hoda Team | misbahulhoda.org</p>
                      </div>
                    </div>
                """);
            });
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Application marked as Under Review."));
    }

    [HttpPatch("pilgrims/{id}/approve")]
    public async Task<IActionResult> ApprovePilgrim(Guid id, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(id, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Not found."));

        pilgrim.Status = ApplicationStatus.Approved;
        unitOfWork.Pilgrims.Update(pilgrim);

        await unitOfWork.Notifications.AddAsync(new Notification
        {
            UserId = pilgrim.UserId, Title = "🎉 Application Approved — Mubarak!",
            Message = "Mubarak! Your Arbaeen 2026 application has been approved. Room and bus allocation will be arranged shortly. Login to your portal for details.",
            Type = NotificationType.Push, Event = NotificationEvent.Approval
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await unitOfWork.Users.GetByIdAsync(pilgrim.UserId, cancellationToken);
        if (user is not null)
        {
            _ = Task.Run(async () =>
            {
                if (!string.IsNullOrEmpty(user.WhatsAppNumber))
                    await whatsApp.SendMessageAsync(user.WhatsAppNumber,
                        $"Assalamu Alaikum {user.FullName},\n\n🎉 *MUBARAK!* Your Arbaeen 2026 application has been *APPROVED*!\n\n✅ Room and bus allocation will be arranged shortly.\n📱 Login to your portal for full details.\n\n*يَا حُسَيْن*\n— Misbah ul Hoda Team 🕌");

                await email.SendAsync(user.Email, user.FullName, "🎉 Application Approved — Misbah ul Hoda", $"""
                    <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;background:#010f05;color:#f1f5f9;border-radius:12px;overflow:hidden;">
                      <div style="background:linear-gradient(135deg,#065f46,#047857,#059669);padding:32px 24px;text-align:center;">
                        <div style="font-size:48px;margin-bottom:8px;">🎉</div>
                        <h1 style="color:#fff;margin:0;font-size:24px;">MUBARAK! Application Approved</h1>
                        <p style="color:#a7f3d0;margin:8px 0 0;">Arbaeen 2026 — Misbah ul Hoda</p>
                      </div>
                      <div style="padding:28px 24px;">
                        <p style="color:#d1fae5;font-size:16px;">Assalamu Alaikum <strong>{user.FullName}</strong>,</p>
                        <p style="color:#94a3b8;font-size:15px;">We are delighted to inform you that your Arbaeen 2026 pilgrimage application has been <strong style="color:#34d399;">APPROVED</strong>!</p>
                        <div style="background:rgba(16,185,129,0.12);border:1px solid rgba(16,185,129,0.4);border-radius:10px;padding:20px;margin:20px 0;text-align:center;">
                          <p style="color:#6ee7b7;font-size:20px;margin:0;font-weight:bold;">✅ Status: APPROVED</p>
                          <p style="color:#a7f3d0;font-size:13px;margin:8px 0 0;">يَا صَاحِبَ الزَّمَان — Your journey to Karbala has begun</p>
                        </div>
                        <p style="color:#94a3b8;"><strong style="color:#d1fae5;">Next Steps:</strong></p>
                        <ul style="color:#94a3b8;line-height:1.8;padding-left:20px;">
                          <li>Room allocation will be assigned shortly</li>
                          <li>Bus details will be sent once confirmed</li>
                          <li>Upload your visa once issued</li>
                          <li>Login to your portal to track all updates</li>
                        </ul>
                        <div style="text-align:center;margin:24px 0;">
                          <a href="https://app.misbahulhoda.org/pilgrim/portal" style="background:linear-gradient(135deg,#065f46,#047857);color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px;">Open My Portal →</a>
                        </div>
                        <p style="color:#6b7280;font-size:13px;border-top:1px solid rgba(16,185,129,0.1);padding-top:16px;margin-top:16px;">— Misbah ul Hoda Team | misbahulhoda.org</p>
                      </div>
                    </div>
                """);
            });
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Application approved. Email and WhatsApp sent."));
    }

    [HttpPatch("pilgrims/{id}/reject")]
    public async Task<IActionResult> RejectPilgrim(Guid id, [FromBody] RejectRequest request, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(id, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Not found."));

        pilgrim.Status = ApplicationStatus.Rejected;
        pilgrim.RejectionReason = request.Reason;
        unitOfWork.Pilgrims.Update(pilgrim);

        await unitOfWork.Notifications.AddAsync(new Notification
        {
            UserId = pilgrim.UserId, Title = "Application Status Update",
            Message = $"Your Arbaeen 2026 application could not be approved at this time. Reason: {request.Reason}. Please contact us for further assistance.",
            Type = NotificationType.Push, Event = NotificationEvent.Approval
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await unitOfWork.Users.GetByIdAsync(pilgrim.UserId, cancellationToken);
        if (user is not null)
        {
            _ = Task.Run(async () =>
            {
                if (!string.IsNullOrEmpty(user.WhatsAppNumber))
                    await whatsApp.SendMessageAsync(user.WhatsAppNumber,
                        $"Assalamu Alaikum {user.FullName},\n\nWe regret to inform you that your Arbaeen 2026 application could not be approved at this time.\n\n*Reason:* {request.Reason}\n\nPlease contact us at info@misbahulhoda.org for further assistance.\n\n— Misbah ul Hoda Team 🕌");

                await email.SendAsync(user.Email, user.FullName, "Application Status Update — Misbah ul Hoda", $"""
                    <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;background:#010f05;color:#f1f5f9;border-radius:12px;overflow:hidden;">
                      <div style="background:linear-gradient(135deg,#7f1d1d,#991b1b);padding:32px 24px;text-align:center;">
                        <h1 style="color:#fff;margin:0;font-size:22px;">Application Status Update</h1>
                        <p style="color:#fca5a5;margin:8px 0 0;">Arbaeen 2026 — Misbah ul Hoda</p>
                      </div>
                      <div style="padding:28px 24px;">
                        <p style="color:#f1f5f9;font-size:16px;">Assalamu Alaikum <strong>{user.FullName}</strong>,</p>
                        <p style="color:#94a3b8;">We regret to inform you that your Arbaeen 2026 application could not be approved at this time.</p>
                        <div style="background:rgba(239,68,68,0.1);border:1px solid rgba(239,68,68,0.3);border-radius:8px;padding:16px;margin:20px 0;">
                          <p style="color:#fca5a5;margin:0 0 8px;font-weight:bold;">Reason for non-approval:</p>
                          <p style="color:#f87171;margin:0;font-style:italic;">"{request.Reason}"</p>
                        </div>
                        <p style="color:#94a3b8;">If you believe this is a mistake or would like to appeal, please contact us:</p>
                        <p style="color:#6ee7b7;">📧 info@misbahulhoda.org<br>📱 WhatsApp: Available on our website</p>
                        <p style="color:#6b7280;font-size:13px;border-top:1px solid rgba(239,68,68,0.1);padding-top:16px;margin-top:16px;">— Misbah ul Hoda Team | misbahulhoda.org</p>
                      </div>
                    </div>
                """);
            });
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Application rejected. Notification sent."));
    }

    [HttpPatch("pilgrims/{id}/reset-to-pending")]
    public async Task<IActionResult> ResetToPending(Guid id, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(id, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        pilgrim.Status = ApplicationStatus.Pending;
        pilgrim.RejectionReason = null;
        unitOfWork.Pilgrims.Update(pilgrim);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Reset to Pending."));
    }

    // ─── EMERGENCY BROADCAST ───────────────────────────────────────────────────
    [HttpPost("emergency/broadcast")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> EmergencyBroadcast([FromBody] EmergencyBroadcastRequest request, CancellationToken cancellationToken)
    {
        var users = await unitOfWork.Users.FindAsync(u => u.IsActive, cancellationToken);
        var notifications = users.Select(u => new Notification
        {
            UserId = u.Id, Title = $"🚨 EMERGENCY: {request.Title}",
            Message = request.Message, Type = NotificationType.Push, Event = NotificationEvent.EmergencyAlert
        }).ToList();

        foreach (var n in notifications)
            await unitOfWork.Notifications.AddAsync(n, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.SendWhatsApp)
        {
            var phones = users.Where(u => !string.IsNullOrEmpty(u.WhatsAppNumber))
                              .Select(u => u.WhatsAppNumber!).ToList();
            var msg = $"🚨 EMERGENCY ALERT 🚨\n\n{request.Title}\n\n{request.Message}\n\n- Misbahuda Emergency Team";
            _ = whatsApp.BroadcastAsync(phones, msg);
        }

        return Ok(ApiResponse<object>.Ok(new { NotifiedCount = notifications.Count }, $"Emergency alert sent to {notifications.Count} users."));
    }

    // ─── VOLUNTEER STATS (for dashboard) ──────────────────────────────────────
    [HttpGet("volunteers/live-status")]
    public async Task<IActionResult> GetVolunteerLiveStatus(CancellationToken cancellationToken)
    {
        var vols = await unitOfWork.Volunteers.GetAllAsync(cancellationToken);
        var userIds = vols.Select(v => v.UserId).ToList();
        var users = await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken);
        var uMap = users.ToDictionary(u => u.Id);
        var zones = await unitOfWork.Zones.GetAllAsync(cancellationToken);
        var zMap = zones.ToDictionary(z => z.Id);

        var result = vols.Select(v =>
        {
            uMap.TryGetValue(v.UserId, out var u);
            zMap.TryGetValue(v.ZoneId ?? Guid.Empty, out var z);
            return new
            {
                v.Id, Name = u?.FullName ?? "", Status = v.Status.ToString(), v.IsCheckedIn,
                v.LastCheckIn, Zone = z?.Name ?? v.AssignedArea ?? "Unassigned",
                v.TotalTasksCompleted
            };
        }).OrderBy(v => v.Zone);

        return Ok(ApiResponse<object>.Ok(new
        {
            Total = vols.Count(),
            CheckedIn = vols.Count(v => v.IsCheckedIn),
            Available = vols.Count(v => v.Status == VolunteerStatus.Available),
            Busy = vols.Count(v => v.Status == VolunteerStatus.Busy),
            Offline = vols.Count(v => v.Status == VolunteerStatus.Offline),
            Emergency = vols.Count(v => v.Status == VolunteerStatus.EmergencyAssigned),
            Volunteers = result
        }));
    }
}

public record AllocateRoomRequest(Guid RoomId);
public record AllocateBusRequest(Guid BusId);
public record RejectRequest(string Reason);
public record EmergencyBroadcastRequest(string Title, string Message, bool SendWhatsApp);

public record CreateUserRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string? WhatsAppNumber,
    Misbahuda.Domain.Enums.UserRole Role
);

public record ChangeRoleRequest(Misbahuda.Domain.Enums.UserRole Role);
