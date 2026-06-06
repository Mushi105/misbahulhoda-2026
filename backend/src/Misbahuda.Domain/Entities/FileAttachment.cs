using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class FileAttachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileType { get; set; } = string.Empty;

    public Guid? PilgrimId { get; set; }
    public Pilgrim? Pilgrim { get; set; }
}
