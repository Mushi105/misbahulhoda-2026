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

[Authorize]
public class NotificationsController(
    IMediator mediator,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    NotificationDispatcher dispatcher)
    : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();

        var notifications = await unitOfWork.Notifications.FindAsync(
            n => n.UserId == currentUser.UserId, cancellationToken);

        return Ok(ApiResponse<IEnumerable<Notification>>.Ok(
            notifications.OrderByDescending(n => n.CreatedAt)));
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var notification = await unitOfWork.Notifications.GetByIdAsync(id, cancellationToken);
        if (notification is null || notification.UserId != currentUser.UserId)
            return NotFound(ApiResponse<object>.Fail("Notification not found."));

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        unitOfWork.Notifications.Update(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Marked as read."));
    }

    /// <summary>
    /// Send to a single user with optional WhatsApp + Email delivery.
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return NotFound(ApiResponse<object>.Fail("User not found."));

        await dispatcher.SendAsync(
            user, request.Title, request.Message, request.Event,
            sendWhatsApp: request.SendWhatsApp,
            sendEmail: request.SendEmail,
            cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Notification sent."));
    }

    /// <summary>
    /// Broadcast to all users matching a role (or all) with optional WhatsApp + Email.
    /// </summary>
    [HttpPost("broadcast")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest request, CancellationToken cancellationToken)
    {
        IEnumerable<User> users = await unitOfWork.Users.GetAllAsync(cancellationToken);

        if (!string.IsNullOrEmpty(request.Role) &&
            Enum.TryParse<UserRole>(request.Role, out var role))
            users = users.Where(u => u.Role == role);

        var userList = users.Where(u => u.IsActive).ToList();
        if (!userList.Any())
            return BadRequest(ApiResponse<object>.Fail("No users found for this role."));

        await dispatcher.BroadcastAsync(
            userList, request.Title, request.Message, request.Event,
            sendWhatsApp: request.SendWhatsApp,
            sendEmail: request.SendEmail,
            cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { Count = userList.Count },
            $"Broadcast sent to {userList.Count} users."));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();

        var notifications = await unitOfWork.Notifications.FindAsync(
            n => n.UserId == currentUser.UserId && !n.IsRead, cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { Count = notifications.Count() }));
    }
}

public record SendNotificationRequest(
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type,
    NotificationEvent Event,
    bool SendWhatsApp = false,
    bool SendEmail = false
);

public record BroadcastRequest(
    string Title,
    string Message,
    NotificationEvent Event,
    string? Role,           // null = all, or "Pilgrim", "Volunteer", etc.
    bool SendWhatsApp = false,
    bool SendEmail = false
);
