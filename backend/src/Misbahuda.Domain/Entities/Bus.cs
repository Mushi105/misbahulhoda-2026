using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class Bus : BaseEntity
{
    public string BusNumber { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentPassengers { get; set; } = 0;
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? KarwanId { get; set; }
    public Karwan? Karwan { get; set; }
    public Guid? DriverUserId { get; set; }
    public ICollection<Pilgrim> Pilgrims { get; set; } = [];
    public ICollection<GpsLocation> GpsLocations { get; set; } = [];
}
