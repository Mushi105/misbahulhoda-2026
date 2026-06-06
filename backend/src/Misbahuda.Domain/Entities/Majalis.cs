using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class Majalis : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Language { get; set; } = "Urdu";
    public string? MolanaName { get; set; }
    public string? NohaKhuwanName { get; set; }
    public string Venue { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ReminderSentAt { get; set; }
}

public class NamazTiming : BaseEntity
{
    public string PrayerName { get; set; } = string.Empty;
    public TimeOnly Time { get; set; }
    public DateTime Date { get; set; }
    public string? Venue { get; set; }
}

public class FoodSchedule : BaseEntity
{
    public string MealType { get; set; } = string.Empty;
    public DateTime ServedAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedServings { get; set; }
}

/// <summary>
/// Individual program item within a Majalis (Hadith, Marsiya, Salam, Noha, etc.)
/// </summary>
public class MajalisProgram : BaseEntity
{
    public Guid MajalisId { get; set; }
    public Majalis Majalis { get; set; } = null!;

    public string Type { get; set; } = string.Empty; // Hadith | Marsiya | Salam | Noha | Opening | Closing | Break
    public string Time { get; set; } = string.Empty; // "08:15"
    public string? PerformerName { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
}
