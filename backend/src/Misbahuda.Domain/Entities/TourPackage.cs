using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class TourPackage : BaseEntity
{
    public Guid    TourId      { get; set; }
    public Tour    Tour        { get; set; } = null!;
    public string  PackageName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price       { get; set; }
    public string  Currency    { get; set; } = "USD";
    public int     MaxPilgrims { get; set; }
    public int     DurationDays { get; set; }

    // Trip classification
    public TripDuration TripDuration { get; set; } = TripDuration.Short;
    public string       Destinations { get; set; } = string.Empty; // comma-separated e.g. "Iraq,Iran,Sham"
    public string?      RouteDetail  { get; set; } // e.g. "Islamabad → Mashhad → Najaf → Karbala"

    public bool    IncludesAirportTransfer { get; set; }
    public bool    IncludesBreakfast       { get; set; }
    public bool    IncludesLunch           { get; set; }
    public bool    IncludesDinner          { get; set; }
    public string? HotelInfo   { get; set; }
    public string? Notes       { get; set; }
    public bool    IsActive    { get; set; } = true;

    public ICollection<Pilgrim> Pilgrims { get; set; } = [];
}
