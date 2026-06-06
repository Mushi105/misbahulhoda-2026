using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class Room : BaseEntity
{
    public string RoomNumber { get; set; } = string.Empty;
    public int BedCapacity { get; set; }
    public int OccupiedBeds { get; set; } = 0;
    public bool IsForFamily { get; set; } = false;
    public bool IsAvailable => OccupiedBeds < BedCapacity;

    public Guid FloorId { get; set; }
    public Floor Floor { get; set; } = null!;
    public ICollection<Pilgrim> Pilgrims { get; set; } = [];
}
