using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class DeviceToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
