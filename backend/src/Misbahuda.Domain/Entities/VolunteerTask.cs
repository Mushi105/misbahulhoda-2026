using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class VolunteerTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskCategory Category { get; set; }
    public VolunteerTaskStatus Status { get; set; } = VolunteerTaskStatus.Assigned;
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsEmergency { get; set; } = false;
    public string? Notes { get; set; }

    public Guid VolunteerId { get; set; }
    public Volunteer Volunteer { get; set; } = null!;
    public Guid AssignedByUserId { get; set; }
}
