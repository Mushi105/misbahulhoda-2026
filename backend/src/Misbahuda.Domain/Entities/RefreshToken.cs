using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? RevokedReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
