using MediatR;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Auth;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password, string? IpAddress, string? UserAgent)
    : IRequest<ApiResponse<AuthResponse>>;

public class LoginCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
    : IRequestHandler<LoginCommand, ApiResponse<AuthResponse>>
{
    public async Task<ApiResponse<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email.ToLowerInvariant() && !u.IsDeleted, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

        if (!user.IsActive)
            return ApiResponse<AuthResponse>.Fail("Account is deactivated. Please contact support.");

        var refreshTokenValue = jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };

        await unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        user.LastLoginAt = DateTime.UtcNow;
        unitOfWork.Users.Update(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtService.GenerateAccessToken(user);

        return ApiResponse<AuthResponse>.Ok(new AuthResponse(
            accessToken,
            refreshTokenValue,
            DateTime.UtcNow.AddMinutes(60),
            new UserDto(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role.ToString(), user.IsActive)
        ), "Login successful.");
    }
}
