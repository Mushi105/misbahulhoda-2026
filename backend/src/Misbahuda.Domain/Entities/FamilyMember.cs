using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class FamilyMember : BaseEntity
{
    public Guid PilgrimId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public Relationship Relationship { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PassportNumber { get; set; }
    public string? VisaNumber { get; set; }
    public string? Nationality { get; set; }
    public bool RequiresWheelchair { get; set; } = false;
    public bool IsMinor { get; set; } = false;
    public string? SpecialNotes { get; set; }

    public Pilgrim Pilgrim { get; set; } = null!;
}
