using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Pilgrim;
using Misbahuda.Application.Features.Pilgrims.Commands;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class PilgrimsController(IMediator mediator, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : BaseController(mediator)
{
    [HttpPost("profile")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> CreateProfile([FromBody] CreatePilgrimRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePilgrimProfileCommand(
            request.PassportNumber, request.VisaNumber, request.Country,
            request.FamilyMemberCount, request.ArrivalDate, request.DepartureDate,
            request.ArrivalFlight, request.DepartureFlight,
            request.EmergencyContactName, request.EmergencyContactPhone);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("profile/me")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var pilgrim = (await unitOfWork.Pilgrims.FindAsync(p => p.UserId == currentUser.UserId, cancellationToken)).FirstOrDefault();
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Profile not found. Please complete your profile."));

        var user = await unitOfWork.Users.GetByIdAsync(pilgrim.UserId, cancellationToken);
        var familyMembers = await unitOfWork.FamilyMembers.FindAsync(f => f.PilgrimId == pilgrim.Id, cancellationToken);
        var room = pilgrim.RoomId.HasValue ? await unitOfWork.Rooms.GetByIdAsync(pilgrim.RoomId.Value, cancellationToken) : null;
        var bus = pilgrim.BusId.HasValue ? await unitOfWork.Buses.GetByIdAsync(pilgrim.BusId.Value, cancellationToken) : null;

        // Load hotel info for room
        object? roomData = null;
        if (room is not null)
        {
            var floor = await unitOfWork.Floors.GetByIdAsync(room.FloorId, cancellationToken);
            string hotelName = "", hotelAddress = "", hotelCity = "", hotelPhone = "";
            double? lat = null, lng = null;
            string? nearHaram = null, haramDistanceText = null;
            double? haramLat = null, haramLng = null;
            if (floor is not null)
            {
                var building = await unitOfWork.Buildings.GetByIdAsync(floor.BuildingId, cancellationToken);
                if (building is not null)
                {
                    var hotel = await unitOfWork.Hotels.GetByIdAsync(building.HotelId, cancellationToken);
                    if (hotel is not null)
                    {
                        hotelName = hotel.Name;
                        hotelAddress = hotel.Address;
                        hotelCity = hotel.City;
                        hotelPhone = hotel.PhoneNumber ?? "";
                        lat = hotel.Latitude;
                        lng = hotel.Longitude;
                        nearHaram = hotel.NearHaram;
                        haramDistanceText = hotel.HaramDistanceText;
                        haramLat = hotel.HaramLatitude;
                        haramLng = hotel.HaramLongitude;
                    }
                }
            }
            roomData = new
            {
                room.Id, room.RoomNumber, room.BedCapacity,
                FloorNumber = floor?.FloorNumber ?? 0,
                FloorLabel = floor?.Label ?? "",
                HotelName = hotelName,
                HotelAddress = hotelAddress,
                HotelCity = hotelCity,
                HotelPhone = hotelPhone,
                Latitude = lat,
                Longitude = lng,
                NearHaram = nearHaram,
                HaramDistanceText = haramDistanceText,
                HaramLatitude = haramLat,
                HaramLongitude = haramLng
            };
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            pilgrim.Id, pilgrim.PassportNumber, pilgrim.VisaNumber, pilgrim.Country,
            pilgrim.FamilyMemberCount, pilgrim.ArrivalDate, pilgrim.DepartureDate,
            pilgrim.ArrivalFlight, pilgrim.DepartureFlight, Status = pilgrim.Status.ToString(), pilgrim.RejectionReason,
            pilgrim.EmergencyContactName, pilgrim.EmergencyContactPhone,
            User = user is null ? null : new { user.FullName, user.Email, user.PhoneNumber, user.WhatsAppNumber },
            Room = roomData,
            Bus = bus is null ? null : new { bus.Id, bus.BusNumber, bus.PlateNumber, bus.DriverName, bus.DriverPhone },
            FamilyMembers = familyMembers.OrderBy(f => f.CreatedAt).Select(f => new
            {
                f.Id, f.FullName, f.Gender, f.Relationship, f.DateOfBirth,
                f.PassportNumber, f.VisaNumber, f.Nationality,
                f.RequiresWheelchair, f.IsMinor, f.SpecialNotes
            })
        }));
    }

    [HttpPost("family-members")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> AddFamilyMember([FromBody] AddFamilyMemberRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var pilgrim = (await unitOfWork.Pilgrims.FindAsync(p => p.UserId == currentUser.UserId, cancellationToken)).FirstOrDefault();
        if (pilgrim is null) return BadRequest(ApiResponse<object>.Fail("Complete your pilgrim profile first."));

        var member = new FamilyMember
        {
            PilgrimId = pilgrim.Id,
            FullName = request.FullName,
            Gender = request.Gender,
            Relationship = request.Relationship,
            DateOfBirth = request.DateOfBirth.HasValue
                ? DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc)
                : null,
            PassportNumber = request.PassportNumber,
            VisaNumber = request.VisaNumber,
            Nationality = request.Nationality,
            RequiresWheelchair = request.RequiresWheelchair,
            IsMinor = request.IsMinor,
            SpecialNotes = request.SpecialNotes
        };

        await unitOfWork.FamilyMembers.AddAsync(member, cancellationToken);
        pilgrim.FamilyMemberCount = (await unitOfWork.FamilyMembers.FindAsync(f => f.PilgrimId == pilgrim.Id, cancellationToken)).Count() + 1;
        unitOfWork.Pilgrims.Update(pilgrim);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { member.Id }, "Family member added."));
    }

    [HttpPut("family-members/{memberId}")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> UpdateFamilyMember(Guid memberId, [FromBody] AddFamilyMemberRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var pilgrim = (await unitOfWork.Pilgrims.FindAsync(p => p.UserId == currentUser.UserId, cancellationToken)).FirstOrDefault();
        if (pilgrim is null) return NotFound();

        var member = await unitOfWork.FamilyMembers.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.PilgrimId != pilgrim.Id)
            return NotFound(ApiResponse<object>.Fail("Family member not found."));

        member.FullName = request.FullName;
        member.Gender = request.Gender;
        member.Relationship = request.Relationship;
        member.DateOfBirth = request.DateOfBirth.HasValue
            ? DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc)
            : null;
        member.PassportNumber = request.PassportNumber;
        member.VisaNumber = request.VisaNumber;
        member.Nationality = request.Nationality;
        member.RequiresWheelchair = request.RequiresWheelchair;
        member.IsMinor = request.IsMinor;
        member.SpecialNotes = request.SpecialNotes;

        unitOfWork.FamilyMembers.Update(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { member.Id }, "Family member updated."));
    }

    [HttpDelete("family-members/{memberId}")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> RemoveFamilyMember(Guid memberId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var pilgrim = (await unitOfWork.Pilgrims.FindAsync(p => p.UserId == currentUser.UserId, cancellationToken)).FirstOrDefault();
        if (pilgrim is null) return NotFound();

        var member = await unitOfWork.FamilyMembers.GetByIdAsync(memberId, cancellationToken);
        if (member is null || member.PilgrimId != pilgrim.Id)
            return NotFound(ApiResponse<object>.Fail("Family member not found."));

        unitOfWork.FamilyMembers.Delete(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Family member removed."));
    }

    [HttpPost("checkout")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> CheckOut(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var pilgrim = (await unitOfWork.Pilgrims.FindAsync(p => p.UserId == currentUser.UserId, cancellationToken)).FirstOrDefault();
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Profile not found."));
        if (!pilgrim.RoomId.HasValue) return BadRequest(ApiResponse<object>.Fail("You are not assigned to any room."));

        var room = await unitOfWork.Rooms.GetByIdAsync(pilgrim.RoomId.Value, cancellationToken);

        // Resolve hotel name for notifications
        string hotelName = "", roomNumber = room?.RoomNumber ?? "", floorLabel = "", city = "";
        if (room is not null)
        {
            var floor = await unitOfWork.Floors.GetByIdAsync(room.FloorId, cancellationToken);
            if (floor is not null)
            {
                floorLabel = floor.Label ?? $"Floor {floor.FloorNumber}";
                var building = await unitOfWork.Buildings.GetByIdAsync(floor.BuildingId, cancellationToken);
                if (building is not null)
                {
                    var hotel = await unitOfWork.Hotels.GetByIdAsync(building.HotelId, cancellationToken);
                    if (hotel is not null) { hotelName = hotel.Name; city = hotel.City; }
                }
            }
            if (room.OccupiedBeds > 0) room.OccupiedBeds--;
            unitOfWork.Rooms.Update(room);
        }

        var leavingRoomId = pilgrim.RoomId!.Value;
        pilgrim.RoomId = null;
        unitOfWork.Pilgrims.Update(pilgrim);

        // Close history record
        var openHistory = (await unitOfWork.PilgrimRoomHistories.FindAsync(
            h => h.PilgrimId == pilgrim.Id && h.RoomId == leavingRoomId && h.CheckedOutAt == null, cancellationToken))
            .FirstOrDefault();
        if (openHistory is not null)
        {
            openHistory.CheckedOutAt = DateTime.UtcNow;
            openHistory.CheckedOutBy = "Self";
            unitOfWork.PilgrimRoomHistories.Update(openHistory);
        }

        // Notify all admins
        var pilgrimUser = await unitOfWork.Users.GetByIdAsync(currentUser.UserId.Value, cancellationToken);
        var pilgrimName = pilgrimUser?.FullName ?? "A pilgrim";
        var adminUsers = await unitOfWork.Users.FindAsync(
            u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin, cancellationToken);

        var adminNotifs = adminUsers.Select(admin => new Notification
        {
            UserId = admin.Id,
            Title = "🚪 Room Checked Out",
            Message = $"{pilgrimName} has checked out of Room {roomNumber} ({floorLabel}) at {hotelName}, {city}. Room is now available.",
            Type = NotificationType.Push,
            Event = NotificationEvent.RoomAllocation
        }).ToList();

        foreach (var n in adminNotifs) await unitOfWork.Notifications.AddAsync(n, cancellationToken);

        // Confirm to pilgrim
        await unitOfWork.Notifications.AddAsync(new Notification
        {
            UserId = currentUser.UserId.Value,
            Title = "✅ Checked Out",
            Message = $"You have successfully checked out of Room {roomNumber} at {hotelName}, {city}. The team will assign your next room when you arrive.",
            Type = NotificationType.Push,
            Event = NotificationEvent.RoomAllocation
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { hotelName, roomNumber }, "Checked out successfully."));
    }
}

public record AddFamilyMemberRequest(
    string FullName,
    Gender Gender,
    Relationship Relationship,
    DateTime? DateOfBirth,
    string? PassportNumber,
    string? VisaNumber,
    string? Nationality,
    bool RequiresWheelchair,
    bool IsMinor,
    string? SpecialNotes
);
