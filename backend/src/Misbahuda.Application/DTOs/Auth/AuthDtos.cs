namespace Misbahuda.Application.DTOs.Auth;

public record RegisterRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string? WhatsAppNumber,
    string? Address1,
    string? Address2,
    string? City,
    string? Country,
    int? Gender,
    string? DateOfBirth,
    string? PassportNumber,
    string? PassportExpiry,
    int? FamilyMemberCount,
    int? PackageType
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    bool IsActive
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
