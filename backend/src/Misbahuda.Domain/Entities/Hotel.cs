using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class Hotel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ContactPerson { get; set; }
    public int TotalBuildings { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Display metadata — managed from admin panel
    public string? NightsLabel { get; set; }           // e.g. "2 nights (Nights 1–3)"
    public string? NearHaram { get; set; }             // e.g. "5 min walk to Haram of Kadhimain"
    public string? HaramDistanceText { get; set; }     // e.g. "~400 m from Kadhimain Shrine"
    public double? HaramLatitude { get; set; }
    public double? HaramLongitude { get; set; }
    public string? IconEmoji { get; set; }             // e.g. "🕌"
    public string? ColorClass { get; set; }            // e.g. "border-gold-700 bg-gold-950"
    public string? Amenities { get; set; }             // comma-separated
    public string? Tips { get; set; }

    // Jamat (congregational prayer) timings
    public string? JamatFajr { get; set; }
    public string? AdhanFajr { get; set; }
    public string? JamatZuhr { get; set; }
    public string? AdhanZuhr { get; set; }
    public string? JamatAsr { get; set; }
    public string? AdhanAsr { get; set; }
    public string? JamatMaghrib { get; set; }
    public string? AdhanMaghrib { get; set; }
    public string? JamatIsha { get; set; }
    public string? AdhanIsha { get; set; }

    public ICollection<Building> Buildings { get; set; } = [];
}

public class PilgrimRoomHistory : BaseEntity
{
    public Guid PilgrimId { get; set; }
    public Pilgrim Pilgrim { get; set; } = null!;

    // Snapshot at time of assignment — stored directly so history survives room/hotel changes
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string FloorLabel { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string HotelCity { get; set; } = string.Empty;

    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedOutAt { get; set; }

    // "Self" = pilgrim checked out, "Admin" = admin deallocated
    public string CheckedOutBy { get; set; } = string.Empty;
}

public class Building : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int TotalFloors { get; set; }

    public Guid HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;
    public ICollection<Floor> Floors { get; set; } = [];
}

public class Floor : BaseEntity
{
    public int FloorNumber { get; set; }
    public string? Label { get; set; }

    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public ICollection<Room> Rooms { get; set; } = [];
}
