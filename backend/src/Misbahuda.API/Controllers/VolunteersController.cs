using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Volunteer;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class VolunteersController(IMediator mediator, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : BaseController(mediator)
{
    // ─── GET ALL ───────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,VolunteerManager")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var volunteers = await unitOfWork.Volunteers.GetAllAsync(cancellationToken);
        var userIds = volunteers.Select(v => v.UserId).ToList();
        var users = await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);
        var zones = await unitOfWork.Zones.GetAllAsync(cancellationToken);
        var zoneMap = zones.ToDictionary(z => z.Id);

        var dtos = volunteers.Select(v =>
        {
            userMap.TryGetValue(v.UserId, out var u);
            zoneMap.TryGetValue(v.ZoneId ?? Guid.Empty, out var z);
            return new
            {
                v.Id, v.UserId,
                FullName = u?.FullName ?? "",
                Email = u?.Email ?? "",
                Phone = u?.PhoneNumber ?? "",
                WhatsApp = u?.WhatsAppNumber,
                v.Skills, Status = v.Status.ToString(), v.IsCheckedIn,
                v.LastCheckIn, v.LastCheckOut,
                v.TotalTasksCompleted, v.AssignedArea,
                v.ShiftStart, v.ShiftEnd, v.EmergencyPhone,
                ZoneId = v.ZoneId,
                ZoneName = z?.Name,
                ZoneCode = z?.ZoneCode
            };
        });

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.ToLower();
            dtos = dtos.Where(v => v.FullName.ToLower().Contains(s) || v.Email.ToLower().Contains(s));
        }

        var total = dtos.Count();
        var paged = dtos.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
        return Ok(ApiResponse<object>.Ok(new { Items = paged, TotalCount = total }));
    }

    // ─── GET MINE ──────────────────────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize(Roles = "Volunteer,VolunteerManager")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var vol = (await unitOfWork.Volunteers.FindAsync(v => v.UserId == currentUser.UserId, cancellationToken)).FirstOrDefault();
        if (vol is null) return NotFound(ApiResponse<object>.Fail("No volunteer profile found."));

        var user = await unitOfWork.Users.GetByIdAsync(vol.UserId, cancellationToken);
        var zone = vol.ZoneId.HasValue ? await unitOfWork.Zones.GetByIdAsync(vol.ZoneId.Value, cancellationToken) : null;
        var tasks = await unitOfWork.VolunteerTasks.FindAsync(t => t.VolunteerId == vol.Id, cancellationToken);
        var assignments = await unitOfWork.VolunteerPilgrimAssignments.FindAsync(a => a.VolunteerId == vol.Id && a.IsActive, cancellationToken);
        var pilgrimIds = assignments.Select(a => a.PilgrimId).ToList();
        var pilgrims = await unitOfWork.Pilgrims.FindAsync(p => pilgrimIds.Contains(p.Id), cancellationToken);
        var pilgrimUserIds = pilgrims.Select(p => p.UserId).ToList();
        var pilgrimUsers = await unitOfWork.Users.FindAsync(u => pilgrimUserIds.Contains(u.Id), cancellationToken);
        var pugMap = pilgrimUsers.ToDictionary(u => u.Id);

        return Ok(ApiResponse<object>.Ok(new
        {
            vol.Id, vol.UserId, vol.Skills, Status = vol.Status.ToString(), vol.IsCheckedIn,
            vol.LastCheckIn, vol.LastCheckOut, vol.TotalTasksCompleted,
            vol.AssignedArea, vol.ShiftStart, vol.ShiftEnd, vol.EmergencyPhone,
            User = new { user!.FullName, user.Email, user.PhoneNumber, user.WhatsAppNumber },
            Zone = zone is null ? null : new { zone.Id, zone.Name, zone.ZoneCode, zone.Location },
            Tasks = tasks.OrderByDescending(t => t.CreatedAt).Select(t => new
            {
                t.Id, t.Title, t.Description, Category = t.Category.ToString(), Status = t.Status.ToString(),
                t.DueAt, t.CompletedAt, t.IsEmergency, t.Notes
            }),
            AssignedPilgrims = pilgrims.Select(p =>
            {
                pugMap.TryGetValue(p.UserId, out var pu);
                return new
                {
                    p.Id, Status = p.Status.ToString(), p.Country, p.FamilyMemberCount,
                    FullName = pu?.FullName ?? "", Phone = pu?.PhoneNumber ?? "",
                    WhatsApp = pu?.WhatsAppNumber
                };
            })
        }));
    }

    // ─── CHECK IN ──────────────────────────────────────────────────────────────
    [HttpPost("{id}/checkin")]
    [Authorize(Roles = "Volunteer,VolunteerManager")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
    {
        var vol = await unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (vol is null) return NotFound();
        vol.IsCheckedIn = true;
        vol.LastCheckIn = DateTime.UtcNow;
        vol.Status = VolunteerStatus.Available;
        unitOfWork.Volunteers.Update(vol);
        await unitOfWork.Attendances.AddAsync(new Attendance { VolunteerId = id, CheckInAt = DateTime.UtcNow }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Checked in. Status: Available."));
    }

    // ─── CHECK OUT ─────────────────────────────────────────────────────────────
    [HttpPost("{id}/checkout")]
    [Authorize(Roles = "Volunteer,VolunteerManager")]
    public async Task<IActionResult> CheckOut(Guid id, CancellationToken cancellationToken)
    {
        var vol = await unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (vol is null) return NotFound();
        vol.IsCheckedIn = false;
        vol.LastCheckOut = DateTime.UtcNow;
        vol.Status = VolunteerStatus.Offline;
        unitOfWork.Volunteers.Update(vol);
        var att = (await unitOfWork.Attendances.FindAsync(a => a.VolunteerId == id && a.CheckOutAt == null, cancellationToken)).FirstOrDefault();
        if (att is not null) { att.CheckOutAt = DateTime.UtcNow; unitOfWork.Attendances.Update(att); }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Checked out."));
    }

    // ─── UPDATE STATUS ─────────────────────────────────────────────────────────
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Volunteer,VolunteerManager,Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateVolunteerStatusRequest request, CancellationToken cancellationToken)
    {
        var vol = await unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (vol is null) return NotFound();
        vol.Status = request.Status;
        unitOfWork.Volunteers.Update(vol);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { vol.Status }, "Status updated."));
    }

    // ─── ASSIGN ZONE ───────────────────────────────────────────────────────────
    [HttpPatch("{id}/zone")]
    [Authorize(Roles = "SuperAdmin,Admin,VolunteerManager")]
    public async Task<IActionResult> AssignZone(Guid id, [FromBody] AssignZoneRequest request, CancellationToken cancellationToken)
    {
        var vol = await unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (vol is null) return NotFound(ApiResponse<object>.Fail("Volunteer not found."));
        var zone = await unitOfWork.Zones.GetByIdAsync(request.ZoneId, cancellationToken);
        if (zone is null) return NotFound(ApiResponse<object>.Fail("Zone not found."));
        vol.ZoneId = request.ZoneId;
        vol.AssignedArea = zone.Name;
        unitOfWork.Volunteers.Update(vol);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { zone.Name, zone.ZoneCode }, $"Assigned to zone: {zone.Name}"));
    }

    // ─── ASSIGN PILGRIM ────────────────────────────────────────────────────────
    [HttpPost("{id}/assign-pilgrim")]
    [Authorize(Roles = "SuperAdmin,Admin,VolunteerManager")]
    public async Task<IActionResult> AssignPilgrim(Guid id, [FromBody] AssignPilgrimRequest request, CancellationToken cancellationToken)
    {
        var vol = await unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (vol is null) return NotFound(ApiResponse<object>.Fail("Volunteer not found."));
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(request.PilgrimId, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));

        var exists = await unitOfWork.VolunteerPilgrimAssignments.ExistsAsync(
            a => a.VolunteerId == id && a.PilgrimId == request.PilgrimId && a.IsActive, cancellationToken);
        if (exists) return BadRequest(ApiResponse<object>.Fail("Already assigned."));

        await unitOfWork.VolunteerPilgrimAssignments.AddAsync(new VolunteerPilgrimAssignment
        {
            VolunteerId = id, PilgrimId = request.PilgrimId, Notes = request.Notes
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Pilgrim assigned to volunteer."));
    }

    // ─── REMOVE PILGRIM ASSIGNMENT ─────────────────────────────────────────────
    [HttpDelete("{id}/assign-pilgrim/{pilgrimId}")]
    [Authorize(Roles = "SuperAdmin,Admin,VolunteerManager")]
    public async Task<IActionResult> RemovePilgrimAssignment(Guid id, Guid pilgrimId, CancellationToken cancellationToken)
    {
        var assignment = (await unitOfWork.VolunteerPilgrimAssignments.FindAsync(
            a => a.VolunteerId == id && a.PilgrimId == pilgrimId, cancellationToken)).FirstOrDefault();
        if (assignment is null) return NotFound();
        assignment.IsActive = false;
        unitOfWork.VolunteerPilgrimAssignments.Update(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Assignment removed."));
    }

    // ─── ASSIGN TASK ───────────────────────────────────────────────────────────
    [HttpPost("tasks/assign")]
    [Authorize(Roles = "SuperAdmin,Admin,VolunteerManager")]
    public async Task<IActionResult> AssignTask([FromBody] AssignTaskRequest request, CancellationToken cancellationToken)
    {
        var vol = await unitOfWork.Volunteers.GetByIdAsync(request.VolunteerId, cancellationToken);
        if (vol is null) return NotFound(ApiResponse<object>.Fail("Volunteer not found."));

        var task = new VolunteerTask
        {
            VolunteerId = request.VolunteerId, Title = request.Title,
            Description = request.Description, Category = request.Category,
            DueAt = request.DueAt, IsEmergency = request.IsEmergency,
            AssignedByUserId = currentUser.UserId!.Value, Notes = request.Notes
        };
        if (request.IsEmergency) vol.Status = VolunteerStatus.EmergencyAssigned;
        await unitOfWork.VolunteerTasks.AddAsync(task, cancellationToken);
        unitOfWork.Volunteers.Update(vol);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { task.Id }, "Task assigned."));
    }

    // ─── COMPLETE TASK ─────────────────────────────────────────────────────────
    [HttpPatch("tasks/{taskId}/complete")]
    [Authorize(Roles = "Volunteer,VolunteerManager,Admin,SuperAdmin")]
    public async Task<IActionResult> CompleteTask(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await unitOfWork.VolunteerTasks.GetByIdAsync(taskId, cancellationToken);
        if (task is null) return NotFound();
        task.Status = Domain.Enums.VolunteerTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;
        unitOfWork.VolunteerTasks.Update(task);
        var vol = await unitOfWork.Volunteers.GetByIdAsync(task.VolunteerId, cancellationToken);
        if (vol is not null) { vol.TotalTasksCompleted++; unitOfWork.Volunteers.Update(vol); }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Task marked complete."));
    }

    // ─── GET TASKS ─────────────────────────────────────────────────────────────
    [HttpGet("{id}/tasks")]
    [Authorize(Roles = "Volunteer,VolunteerManager,Admin,SuperAdmin")]
    public async Task<IActionResult> GetTasks(Guid id, CancellationToken cancellationToken)
    {
        var tasks = await unitOfWork.VolunteerTasks.FindAsync(t => t.VolunteerId == id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(tasks.OrderByDescending(t => t.CreatedAt)));
    }

    // ─── ATTENDANCE LOG ────────────────────────────────────────────────────────
    [HttpGet("{id}/attendance")]
    [Authorize(Roles = "Volunteer,VolunteerManager,Admin,SuperAdmin")]
    public async Task<IActionResult> GetAttendance(Guid id, CancellationToken cancellationToken)
    {
        var att = await unitOfWork.Attendances.FindAsync(a => a.VolunteerId == id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(att.OrderByDescending(a => a.CheckInAt)));
    }

    // ─── PERFORMANCE REPORT ────────────────────────────────────────────────────
    [HttpGet("{id}/report")]
    [Authorize(Roles = "VolunteerManager,Admin,SuperAdmin")]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken cancellationToken)
    {
        var vol = await unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (vol is null) return NotFound();
        var user = await unitOfWork.Users.GetByIdAsync(vol.UserId, cancellationToken);
        var tasks = await unitOfWork.VolunteerTasks.FindAsync(t => t.VolunteerId == id, cancellationToken);
        var att = await unitOfWork.Attendances.FindAsync(a => a.VolunteerId == id, cancellationToken);
        var totalHours = att.Where(a => a.CheckOutAt.HasValue)
            .Sum(a => (a.CheckOutAt!.Value - a.CheckInAt).TotalHours);

        return Ok(ApiResponse<object>.Ok(new
        {
            VolunteerName = user?.FullName,
            Phone = user?.PhoneNumber,
            WhatsApp = user?.WhatsAppNumber,
            Status = vol.Status.ToString(), vol.AssignedArea,
            TotalTasks = tasks.Count(),
            CompletedTasks = tasks.Count(t => t.Status == Domain.Enums.VolunteerTaskStatus.Completed),
            PendingTasks = tasks.Count(t => t.Status == Domain.Enums.VolunteerTaskStatus.Assigned),
            EmergencyTasksHandled = tasks.Count(t => t.IsEmergency && t.Status == Domain.Enums.VolunteerTaskStatus.Completed),
            TotalAttendanceDays = att.Count(),
            TotalHoursWorked = Math.Round(totalHours, 1),
            LastCheckIn = vol.LastCheckIn,
            LastCheckOut = vol.LastCheckOut
        }));
    }

    // ─── CREATE VOLUNTEER PROFILE (Admin creates for existing user) ────────────
    [HttpPost("create")]
    [Authorize(Roles = "SuperAdmin,Admin,VolunteerManager")]
    public async Task<IActionResult> CreateVolunteerProfile([FromBody] CreateVolunteerByAdminRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return NotFound(ApiResponse<object>.Fail("User not found."));

        var exists = await unitOfWork.Volunteers.ExistsAsync(v => v.UserId == request.UserId, cancellationToken);
        if (exists) return BadRequest(ApiResponse<object>.Fail("Volunteer profile already exists for this user."));

        var vol = new Volunteer
        {
            UserId = request.UserId,
            Skills = request.Skills,
            ShiftStart = request.ShiftStart,
            ShiftEnd = request.ShiftEnd,
            AssignedArea = request.AssignedArea,
            EmergencyPhone = request.EmergencyPhone,
            ZoneId = request.ZoneId
        };
        await unitOfWork.Volunteers.AddAsync(vol, cancellationToken);
        user.Role = UserRole.Volunteer;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { vol.Id }, "Volunteer profile created."));
    }

    // ─── UPDATE VOLUNTEER (Admin) ───────────────────────────────────────────────
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,VolunteerManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVolunteerRequest request, CancellationToken cancellationToken)
    {
        var vol = await unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (vol is null) return NotFound();
        vol.Skills = request.Skills;
        vol.ShiftStart = request.ShiftStart;
        vol.ShiftEnd = request.ShiftEnd;
        vol.AssignedArea = request.AssignedArea;
        vol.EmergencyPhone = request.EmergencyPhone;
        if (request.ZoneId.HasValue) vol.ZoneId = request.ZoneId;
        unitOfWork.Volunteers.Update(vol);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Volunteer updated."));
    }

    // ─── PROFILE (self) ────────────────────────────────────────────────────────
    [HttpPost("profile")]
    [Authorize(Roles = "Volunteer")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateVolunteerRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var exists = await unitOfWork.Volunteers.ExistsAsync(v => v.UserId == currentUser.UserId, cancellationToken);
        if (exists) return BadRequest(ApiResponse<object>.Fail("Profile already exists."));
        var vol = new Volunteer
        {
            UserId = currentUser.UserId.Value,
            Skills = request.Skills, ShiftStart = request.ShiftStart,
            ShiftEnd = request.ShiftEnd, AssignedArea = request.AssignedArea
        };
        await unitOfWork.Volunteers.AddAsync(vol, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { vol.Id }, "Volunteer profile created."));
    }
}

// ─── REQUEST RECORDS ──────────────────────────────────────────────────────────
public record AssignZoneRequest(Guid ZoneId);
public record AssignTaskRequest(
    Guid VolunteerId, string Title, string? Description,
    TaskCategory Category, DateTime? DueAt, bool IsEmergency, string? Notes);
public record AssignPilgrimRequest(Guid PilgrimId, string? Notes);
public record CreateVolunteerByAdminRequest(
    Guid UserId, string Skills, string? ShiftStart, string? ShiftEnd,
    string? AssignedArea, string? EmergencyPhone, Guid? ZoneId);
public record UpdateVolunteerRequest(
    string Skills, string? ShiftStart, string? ShiftEnd,
    string? AssignedArea, string? EmergencyPhone, Guid? ZoneId);
