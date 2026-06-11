using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize(Roles = "SuperAdmin,Admin")]
public class AuditLogsController(IMediator mediator, IUnitOfWork unitOfWork) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? entityName = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var all = await unitOfWork.AuditLogs.GetAllAsync(cancellationToken);

        var query = all.AsQueryable();

        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action.Contains(action, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(entityName))
            query = query.Where(l => l.EntityName.Contains(entityName, StringComparison.OrdinalIgnoreCase));

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        var ordered = query.OrderByDescending(l => l.CreatedAt);
        var total = ordered.Count();
        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Get user names
        var userIds = paged.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
        var users = userIds.Any()
            ? await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken)
            : [];
        var userMap = users.ToDictionary(u => u.Id, u => new { u.FullName, u.Email, Role = u.Role.ToString() });

        var result = paged.Select(l => new
        {
            l.Id,
            l.Action,
            l.EntityName,
            l.EntityId,
            Description = l.NewValues,
            l.IpAddress,
            l.CreatedAt,
            User = l.UserId.HasValue && userMap.TryGetValue(l.UserId.Value, out var u)
                ? new { u.FullName, u.Email, u.Role }
                : null
        });

        return Ok(ApiResponse<object>.Ok(new
        {
            Data = result,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        }));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var logs = await unitOfWork.AuditLogs.GetAllAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        return Ok(ApiResponse<object>.Ok(new
        {
            TotalLogs = logs.Count(),
            TodayLogs = logs.Count(l => l.CreatedAt.Date == today),
            LoginCount = logs.Count(l => l.Action == "LOGIN"),
            FailedLogins = logs.Count(l => l.Action == "LOGIN_FAILED"),
            ApprovedPilgrims = logs.Count(l => l.Action == "PILGRIM_APPROVED"),
            RejectedPilgrims = logs.Count(l => l.Action == "PILGRIM_REJECTED"),
            RecentActions = logs.OrderByDescending(l => l.CreatedAt).Take(5)
                .Select(l => new { l.Action, l.EntityName, l.CreatedAt })
        }));
    }
}
