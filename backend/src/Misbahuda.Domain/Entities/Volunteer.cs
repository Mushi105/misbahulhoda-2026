using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class Volunteer : BaseEntity
{
    public Guid UserId { get; set; }
    public string Skills { get; set; } = string.Empty;
    public string? ShiftStart { get; set; }
    public string? ShiftEnd { get; set; }
    public VolunteerStatus Status { get; set; } = VolunteerStatus.Offline;
    public bool IsCheckedIn { get; set; } = false;
    public DateTime? LastCheckIn { get; set; }
    public int TotalTasksCompleted { get; set; } = 0;
    public string? AssignedArea { get; set; }
    public Guid? ZoneId { get; set; }
    public DateTime? LastCheckOut { get; set; }
    public string? EmergencyPhone { get; set; }

    public User User { get; set; } = null!;
    public Zone? Zone { get; set; }
    public ICollection<VolunteerTask> Tasks { get; set; } = [];
    public ICollection<Attendance> Attendances { get; set; } = [];
    public ICollection<VolunteerPilgrimAssignment> PilgrimAssignments { get; set; } = [];
}
