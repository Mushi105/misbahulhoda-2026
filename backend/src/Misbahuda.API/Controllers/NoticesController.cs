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
public class NoticesController(
    IMediator mediator,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    NotificationDispatcher dispatcher)
    : BaseController(mediator)
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var notices = await unitOfWork.Notices.FindAsync(
            n => n.IsActive && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow),
            cancellationToken);

        var result = notices
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Id, n.Title, n.Content, n.Category, n.Priority,
                n.IsPinned, n.ScheduledAt, n.ExpiresAt,
                n.ArabicContent, n.UrduContent, n.CreatedAt
            });

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var notice = await unitOfWork.Notices.GetByIdAsync(id, cancellationToken);
        if (notice is null) return NotFound(ApiResponse<object>.Fail("Notice not found."));
        return Ok(ApiResponse<Notice>.Ok(notice));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateNoticeRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();

        var notice = new Notice
        {
            Title = request.Title,
            Content = request.Content,
            Category = request.Category,
            Priority = request.Priority,
            IsActive = true,
            IsPinned = request.IsPinned,
            ScheduledAt = request.ScheduledAt,
            ExpiresAt = request.ExpiresAt,
            ArabicContent = request.ArabicContent,
            UrduContent = request.UrduContent,
            PostedById = currentUser.UserId.Value
        };

        await unitOfWork.Notices.AddAsync(notice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Optional: broadcast in-app + WhatsApp + Email when notice is published
        if (request.NotifyUsers)
        {
            IEnumerable<User> targets = await unitOfWork.Users.GetAllAsync(cancellationToken);

            if (!string.IsNullOrEmpty(request.NotifyRole) &&
                Enum.TryParse<UserRole>(request.NotifyRole, out var role))
                targets = targets.Where(u => u.Role == role);

            var activeUsers = targets.Where(u => u.IsActive).ToList();

            var notifMessage = $"📋 New notice: {notice.Title}\n\n{notice.Content}";
            await dispatcher.BroadcastAsync(
                activeUsers, notice.Title, notice.Content,
                NotificationEvent.General,
                sendWhatsApp: request.SendWhatsApp,
                sendEmail: request.SendEmail,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse<object>.Ok(new { notice.Id }, "Notice posted."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateNoticeRequest request, CancellationToken cancellationToken)
    {
        var notice = await unitOfWork.Notices.GetByIdAsync(id, cancellationToken);
        if (notice is null) return NotFound(ApiResponse<object>.Fail("Notice not found."));

        notice.Title = request.Title;
        notice.Content = request.Content;
        notice.Category = request.Category;
        notice.Priority = request.Priority;
        notice.IsPinned = request.IsPinned;
        notice.ScheduledAt = request.ScheduledAt;
        notice.ExpiresAt = request.ExpiresAt;
        notice.ArabicContent = request.ArabicContent;
        notice.UrduContent = request.UrduContent;

        unitOfWork.Notices.Update(notice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Notice updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var notice = await unitOfWork.Notices.GetByIdAsync(id, cancellationToken);
        if (notice is null) return NotFound(ApiResponse<object>.Fail("Notice not found."));

        unitOfWork.Notices.Delete(notice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Notice deleted."));
    }

    [HttpPatch("{id}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken cancellationToken)
    {
        var notice = await unitOfWork.Notices.GetByIdAsync(id, cancellationToken);
        if (notice is null) return NotFound(ApiResponse<object>.Fail("Notice not found."));

        notice.IsActive = !notice.IsActive;
        unitOfWork.Notices.Update(notice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { notice.IsActive }));
    }
}

public record CreateNoticeRequest(
    string Title,
    string Content,
    NoticeCategory Category,
    NoticePriority Priority,
    bool IsPinned,
    DateTime? ScheduledAt,
    DateTime? ExpiresAt,
    string? ArabicContent,
    string? UrduContent,
    bool NotifyUsers = false,
    string? NotifyRole = null,   // null = all, or "Pilgrim", "Volunteer", etc.
    bool SendWhatsApp = false,
    bool SendEmail = false
);
