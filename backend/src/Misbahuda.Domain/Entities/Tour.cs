using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class Tour : BaseEntity
{
    public string     TourName    { get; set; } = string.Empty;  // e.g. "Arbaeen 2026"
    public TourType   TourType    { get; set; }
    public int        Year        { get; set; }
    public DateTime   StartDate   { get; set; }
    public DateTime   EndDate     { get; set; }
    public TourStatus Status      { get; set; } = TourStatus.Upcoming;
    public bool       IsOpen      { get; set; } = true;  // accepting new applications
    public string?    Description { get; set; }

    public ICollection<TourPackage>     Packages  { get; set; } = [];
    public ICollection<Pilgrim>         Pilgrims  { get; set; } = [];
    public ICollection<PilgrimFeedback> Feedbacks { get; set; } = [];
    public ICollection<TourDaySchedule> Schedule  { get; set; } = [];
}
