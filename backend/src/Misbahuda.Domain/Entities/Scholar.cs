using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class Scholar : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Language { get; set; } = "English";
    public string Location { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string? Quote { get; set; }
    public string? Tags { get; set; }
    public string? YoutubeSearch { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? Website { get; set; }
    public bool IsReciter { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
