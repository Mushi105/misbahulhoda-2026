using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class Karwan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PoleNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CurrentLocation { get; set; }
    public string? NextStop { get; set; }
    public DateTime? EstimatedArrival { get; set; }

    public ICollection<Bus> Buses { get; set; } = [];
    public ICollection<Pilgrim> Pilgrims { get; set; } = [];
    public ICollection<GpsLocation> GpsLocations { get; set; } = [];
}
