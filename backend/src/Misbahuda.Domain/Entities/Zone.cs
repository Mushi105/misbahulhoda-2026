using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class Zone : BaseEntity
{
    public string Name { get; set; } = string.Empty;          // e.g. "Airport Terminal 1"
    public string Description { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;       // e.g. "ARPT-T1"
    public string? Location { get; set; }                      // address / landmark
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int MaxVolunteers { get; set; } = 5;
    public bool IsActive { get; set; } = true;

    public ICollection<Volunteer> Volunteers { get; set; } = [];
}
