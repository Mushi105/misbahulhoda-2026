using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class TourDaySchedule : BaseEntity
{
    public Guid   TourId       { get; set; }
    public Tour   Tour         { get; set; } = null!;
    public int    DayNumber    { get; set; }   // 1-based
    public DateTime Date       { get; set; }
    public string City         { get; set; } = string.Empty;
    public string Country      { get; set; } = "Iraq";
    public string? Hotel       { get; set; }
    public string? Activity    { get; set; }
    public string? ActivityUrdu { get; set; }
    public string? Notes       { get; set; }
    public bool   IsKeyDay     { get; set; }   // e.g. Arbaeen day, Arbaeen Walk
    public string? Icon        { get; set; }   // emoji for UI
}
