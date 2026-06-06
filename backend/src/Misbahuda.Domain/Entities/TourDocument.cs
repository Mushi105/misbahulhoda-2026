using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class TourDocument : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";   // e.g. Tour Guide, Manual, Notice, Map
    public string FileName { get; set; } = string.Empty; // original file name shown to user
    public string StoredFileName { get; set; } = string.Empty; // unique name on disk
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}
