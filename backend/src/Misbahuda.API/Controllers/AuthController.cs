using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Auth;
using Misbahuda.Application.Features.Auth.Commands;
using Misbahuda.Domain.Interfaces;
using Misbahuda.Infrastructure.Services;

namespace Misbahuda.API.Controllers;

public class AuthController(IMediator mediator, IUnitOfWork unitOfWork, IEmailService emailService) : BaseController(mediator)
{
    /// <summary>Check if email is already registered (real-time validation)</summary>
    [HttpGet("check-email")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckEmail([FromQuery] string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Ok(ApiResponse<object>.Ok(new { exists = false }));
        var exists = await unitOfWork.Users.ExistsAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { exists }));
    }

    /// <summary>Register a new pilgrim account</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.WhatsAppNumber,
            request.Address1,
            request.Address2,
            request.City,
            request.Country,
            request.Gender,
            request.DateOfBirth,
            request.PassportNumber,
            request.PassportExpiry,
            request.FamilyMemberCount,
            request.PackageType
        );
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new LoginCommand(request.Email, request.Password, ipAddress, userAgent);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Refresh access token</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Send password reset link to email</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken cancellationToken)
    {
        var user = (await unitOfWork.Users.FindAsync(u => u.Email == req.Email.ToLower().Trim(), cancellationToken)).FirstOrDefault();

        // Always return success to prevent email enumeration
        if (user is null)
            return Ok(ApiResponse<object>.Ok(new { }, "If this email is registered, a reset link has been sent."));

        // Generate token
        var token   = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        user.PasswordResetToken      = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(2);
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Build reset link — reads frontend URL from config or defaults to localhost
        var frontendUrl = "http://localhost:5173";
        var resetLink   = $"{frontendUrl}/reset-password?token={token}";

        var html = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:1px solid rgba(180,83,9,0.3)">
              <h2 style="color:#d97706;margin-top:0">Password Reset — Misbah ul Hoda</h2>
              <p>Assalam-o-Alaikum <strong>{user.FullName}</strong>,</p>
              <p>We received a request to reset your password. Click the button below to set a new password:</p>
              <div style="text-align:center;margin:28px 0">
                <a href="{resetLink}" style="background:#b45309;color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px">
                  Reset My Password
                </a>
              </div>
              <p style="color:#94a3b8;font-size:13px">This link expires in <strong>2 hours</strong>. If you did not request this, please ignore this email — your password will not change.</p>
              <hr style="border-color:rgba(180,83,9,0.2);margin:20px 0"/>
              <p style="color:#475569;font-size:12px">Misbahuda — Arbaeen 2026 Pilgrimage Management</p>
            </div>
            """;

        await emailService.SendAsync(user.Email, user.FullName, "Reset Your Password — Misbahuda", html);

        return Ok(ApiResponse<object>.Ok(new { }, "If this email is registered, a reset link has been sent."));
    }

    /// <summary>Reset password using token</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken cancellationToken)
    {
        var user = (await unitOfWork.Users.FindAsync(
            u => u.PasswordResetToken == req.Token && u.PasswordResetTokenExpiry > DateTime.UtcNow,
            cancellationToken)).FirstOrDefault();

        if (user is null)
            return BadRequest(ApiResponse<object>.Fail("Reset link is invalid or has expired. Please request a new one."));

        if (req.NewPassword.Length < 8)
            return BadRequest(ApiResponse<object>.Fail("Password must be at least 8 characters."));

        user.PasswordHash            = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.PasswordResetToken      = null;
        user.PasswordResetTokenExpiry = null;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Password reset successfully. You can now sign in."));
    }
}

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);
