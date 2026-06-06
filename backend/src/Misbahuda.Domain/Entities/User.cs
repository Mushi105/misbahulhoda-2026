using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string? ProfilePicture { get; set; }
    public string? WhatsAppNumber { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string?   PasswordResetToken       { get; set; }
    public DateTime? PasswordResetTokenExpiry  { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public Pilgrim? Pilgrim { get; set; }
    public Volunteer? Volunteer { get; set; }
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
