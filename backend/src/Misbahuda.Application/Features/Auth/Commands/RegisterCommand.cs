using MediatR;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Auth;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Application.Features.Auth.Commands;

public record RegisterCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string? WhatsAppNumber,
    string? Address1,
    string? Address2,
    string? City,
    string? Postcode,
    string? Country,
    int? Gender,
    string? DateOfBirth,
    string? PassportNumber,
    string? PassportExpiry,
    int? FamilyMemberCount,
    int? PackageType,
    UserRole Role = UserRole.Pilgrim
) : IRequest<ApiResponse<AuthResponse>>;

public class RegisterCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
    : IRequestHandler<RegisterCommand, ApiResponse<AuthResponse>>
{
    public async Task<ApiResponse<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var exists = await unitOfWork.Users.ExistsAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);
        if (exists)
            return ApiResponse<AuthResponse>.Fail("Email already registered. Please login instead.");

        DateTime? dob = null;
        if (!string.IsNullOrEmpty(request.DateOfBirth) && DateTime.TryParse(request.DateOfBirth, out var parsedDob))
            dob = DateTime.SpecifyKind(parsedDob, DateTimeKind.Utc);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber,
            WhatsAppNumber = request.WhatsAppNumber,
            Address1 = request.Address1,
            Address2 = request.Address2,
            City = request.City,
            Postcode = request.Postcode,
            Country = request.Country,
            Gender = request.Gender.HasValue ? (Gender)request.Gender.Value : null,
            DateOfBirth = dob,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await unitOfWork.Users.AddAsync(user, cancellationToken);

        // Auto-create pilgrim profile
        DateTime? passportExpiry = null;
        if (!string.IsNullOrEmpty(request.PassportExpiry) && DateTime.TryParse(request.PassportExpiry, out var parsedExp))
            passportExpiry = DateTime.SpecifyKind(parsedExp, DateTimeKind.Utc);

        var pilgrim = new Pilgrim
        {
            UserId = user.Id,
            PassportNumber = string.IsNullOrWhiteSpace(request.PassportNumber) ? null : request.PassportNumber,
            PassportExpiry = passportExpiry,
            Country = request.Country ?? string.Empty,
            FamilyMemberCount = request.FamilyMemberCount ?? 1,
            PackageType = request.PackageType.HasValue ? (PackageType)request.PackageType.Value : null,
            ArrivalDate = DateTime.UtcNow.AddMonths(5),
            DepartureDate = DateTime.UtcNow.AddMonths(5).AddDays(14),
        };

        await unitOfWork.Pilgrims.AddAsync(pilgrim, cancellationToken);

        var refreshTokenValue = jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtService.GenerateAccessToken(user);

        return ApiResponse<AuthResponse>.Ok(new AuthResponse(
            accessToken,
            refreshTokenValue,
            DateTime.UtcNow.AddMinutes(60),
            new UserDto(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role.ToString(), user.IsActive)
        ), "Registration successful. Welcome to Misbahuda!");
    }
}
