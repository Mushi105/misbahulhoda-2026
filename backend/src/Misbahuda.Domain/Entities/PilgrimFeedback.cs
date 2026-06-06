using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class PilgrimFeedback : BaseEntity
{
    public Guid    PilgrimId           { get; set; }
    public Pilgrim Pilgrim             { get; set; } = null!;
    public Guid    TourId              { get; set; }
    public Tour    Tour                { get; set; } = null!;

    public int  OverallRating      { get; set; }  // 1–5
    public int? HotelRating        { get; set; }
    public int? TransportRating    { get; set; }
    public int? OrganizationRating { get; set; }
    public int? FoodRating         { get; set; }

    public string? Comment                  { get; set; }
    public string? Suggestions              { get; set; }
    public bool    WouldRecommend           { get; set; }
    public bool    WouldReturn              { get; set; }
    public string? PreferredPackageNextYear { get; set; }
    public DateTime SubmittedAt             { get; set; } = DateTime.UtcNow;
}
