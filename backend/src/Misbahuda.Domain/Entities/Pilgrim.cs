using Misbahuda.Domain.Common;
using Misbahuda.Domain.Enums;

namespace Misbahuda.Domain.Entities;

public class Pilgrim : BaseEntity
{
    public Guid UserId { get; set; }
    public string? PassportNumber { get; set; }
    public DateTime? PassportExpiry { get; set; }
    public string? VisaNumber { get; set; }
    public string Country { get; set; } = string.Empty;
    public PackageType? PackageType { get; set; }
    public int FamilyMemberCount { get; set; } = 1;
    public DateTime ArrivalDate { get; set; }
    public DateTime DepartureDate { get; set; }
    public string? ArrivalFlight    { get; set; }
    public string? DepartureFlight  { get; set; }
    public string? ArrivalAirport   { get; set; }
    public string? DepartureAirport { get; set; }
    public string? ArrivalTime      { get; set; }  // HH:mm  e.g. "14:30"
    public string? DepartureTime    { get; set; }
    public DateTime? ArrivalReminderSentAt     { get; set; }  // 2-day reminder
    public DateTime? DepartureReminderSentAt   { get; set; }  // 2-day reminder
    public DateTime? Arrival5hReminderSentAt   { get; set; }  // 5-hour reminder
    public DateTime? Departure5hReminderSentAt { get; set; }  // 5-hour reminder
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public string? RejectionReason { get; set; }

    // Tour + Package (multi-year support)
    public Guid?        TourId        { get; set; }
    public Tour?        Tour          { get; set; }
    public Guid?        PackageId     { get; set; }
    public TourPackage? Package       { get; set; }

    // Feedback tracking
    public bool      FeedbackSent   { get; set; } = false;
    public DateTime? FeedbackSentAt { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    public User User { get; set; } = null!;
    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }
    public Guid? BusId { get; set; }
    public Bus? Bus { get; set; }
    public Guid? KarwanId { get; set; }
    public Karwan? Karwan { get; set; }
    public ICollection<FileAttachment>   Documents        { get; set; } = [];
    public ICollection<FamilyMember>     FamilyMembers    { get; set; } = [];
    public ICollection<PilgrimRoomHistory> RoomHistory    { get; set; } = [];
    public ICollection<AirportTransfer>  AirportTransfers { get; set; } = [];
    public ICollection<PilgrimFeedback>  Feedbacks        { get; set; } = [];
}
