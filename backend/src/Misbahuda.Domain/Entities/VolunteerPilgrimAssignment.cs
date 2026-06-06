using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class VolunteerPilgrimAssignment : BaseEntity
{
    public Guid VolunteerId { get; set; }
    public Guid PilgrimId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Volunteer Volunteer { get; set; } = null!;
    public Pilgrim Pilgrim { get; set; } = null!;
}
