using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationEvent Event { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
