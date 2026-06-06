using Misbahuda.Domain.Common;

namespace Misbahuda.Domain.Entities;

public class Attendance : BaseEntity
{
    public Guid VolunteerId { get; set; }
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string? Notes { get; set; }

    public Volunteer Volunteer { get; set; } = null!;
}
