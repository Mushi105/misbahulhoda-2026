using Misbahuda.Domain.Enums;

namespace Misbahuda.Application.DTOs.Pilgrim;

public record CreatePilgrimRequest(
    string PassportNumber,
    string? VisaNumber,
    string Country,
    int FamilyMemberCount,
    DateTime ArrivalDate,
    DateTime DepartureDate,
    string? ArrivalFlight,
    string? DepartureFlight,
    string? EmergencyContactName,
    string? EmergencyContactPhone
);

public record UpdatePilgrimRequest(
    string? VisaNumber,
    string? Country,
    int? FamilyMemberCount,
    DateTime? ArrivalDate,
    DateTime? DepartureDate,
    string? ArrivalFlight,
    string? DepartureFlight,
    string? EmergencyContactName,
    string? EmergencyContactPhone
);

public record PilgrimDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string PassportNumber,
    string? VisaNumber,
    string Country,
    int FamilyMemberCount,
    DateTime ArrivalDate,
    DateTime DepartureDate,
    ApplicationStatus Status,
    string? RoomNumber,
    string? BusNumber,
    string? KarwanName
);

public record PilgrimStatusUpdateRequest(
    Guid PilgrimId,
    ApplicationStatus Status,
    string? RejectionReason
);

public record RoomAllocationRequest(
    Guid PilgrimId,
    Guid RoomId
);

public record BusAllocationRequest(
    Guid PilgrimId,
    Guid BusId
);
