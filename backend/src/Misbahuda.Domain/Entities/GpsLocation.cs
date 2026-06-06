using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class GpsLocation : BaseEntity
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public string? Address { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Guid? BusId { get; set; }
    public Bus? Bus { get; set; }
    public Guid? KarwanId { get; set; }
    public Karwan? Karwan { get; set; }
}
