using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class Notice : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NoticeCategory Category { get; set; } = NoticeCategory.General;
    public NoticePriority Priority { get; set; } = NoticePriority.Normal;
    public bool IsActive { get; set; } = true;
    public bool IsPinned { get; set; } = false;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ArabicContent { get; set; }
    public string? UrduContent { get; set; }

    // who posted it (PostedById matches EF shadow FK convention for navigation PostedBy)
    public Guid PostedById { get; set; }
    public User PostedBy { get; set; } = null!;
}
