using MediatR;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Auth;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string AccessToken, string RefreshToken)
    : IRequest<ApiResponse<AuthResponse>>;

public class RefreshTokenCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
    : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponse>>
{
    public async Task<ApiResponse<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = jwtService.ValidateAccessToken(request.AccessToken);
        if (userId is null)
            return ApiResponse<AuthResponse>.Fail("Invalid access token.");

        var storedToken = await unitOfWork.RefreshTokens.FirstOrDefaultAsync(
            t => t.Token == request.RefreshToken && t.UserId == userId && !t.IsRevoked, cancellationToken);

        if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow)
            return ApiResponse<AuthResponse>.Fail("Invalid or expired refresh token.");

        storedToken.IsRevoked = true;
        storedToken.RevokedReason = "Refreshed";
        unitOfWork.RefreshTokens.Update(storedToken);

        var user = await unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || !user.IsActive)
            return ApiResponse<AuthResponse>.Fail("User not found or inactive.");

        var newRefreshTokenValue = jwtService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await unitOfWork.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var newAccessToken = jwtService.GenerateAccessToken(user);

        return ApiResponse<AuthResponse>.Ok(new AuthResponse(
            newAccessToken,
            newRefreshTokenValue,
            DateTime.UtcNow.AddMinutes(60),
            new UserDto(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role.ToString(), user.IsActive)
        ));
    }
}
