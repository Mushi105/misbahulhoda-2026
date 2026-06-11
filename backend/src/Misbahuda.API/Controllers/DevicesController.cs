using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class DevicesController(IMediator mediator, IUnitOfWork unitOfWork, ICurrentUserService currentUser) : BaseController(mediator)
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTokenDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(ApiResponse<object>.Fail("Token is required."));

        var userId = currentUser.UserId!.Value;

        var existing = await unitOfWork.DeviceTokens.FindAsync(
            t => t.UserId == userId && t.Token == dto.Token && !t.IsDeleted, ct);

        if (existing.Any())
        {
            var record = existing.First();
            record.LastSeenAt = DateTime.UtcNow;
            unitOfWork.DeviceTokens.Update(record);
        }
        else
        {
            await unitOfWork.DeviceTokens.AddAsync(new DeviceToken
            {
                UserId      = userId,
                Token       = dto.Token,
                Platform    = dto.Platform ?? "android",
                LastSeenAt  = DateTime.UtcNow
            }, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "Device registered."));
    }

    [HttpDelete("unregister")]
    public async Task<IActionResult> Unregister([FromBody] RegisterTokenDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(ApiResponse<object>.Fail("Token is required."));

        var userId = currentUser.UserId!.Value;
        var tokens = await unitOfWork.DeviceTokens.FindAsync(
            t => t.UserId == userId && t.Token == dto.Token && !t.IsDeleted, ct);

        foreach (var t in tokens)
        {
            t.IsDeleted = true;
            unitOfWork.DeviceTokens.Update(t);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "Device unregistered."));
    }
}

public record RegisterTokenDto(string Token, string? Platform);
