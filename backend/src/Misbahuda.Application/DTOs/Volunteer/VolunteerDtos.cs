using Misbahuda.Domain.Enums;

namespace Misbahuda.Application.DTOs.Volunteer;

public record CreateVolunteerRequest(
    string Skills,
    string? ShiftStart,
    string? ShiftEnd,
    string? AssignedArea
);

public record VolunteerDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string Skills,
    VolunteerStatus Status,
    bool IsCheckedIn,
    int TotalTasksCompleted,
    string? AssignedArea
);

public record AssignTaskRequest(
    Guid VolunteerId,
    string Title,
    string Description,
    TaskCategory Category,
    DateTime? DueAt,
    bool IsEmergency
);

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    TaskCategory Category,
    VolunteerTaskStatus Status,
    DateTime? DueAt,
    bool IsEmergency,
    string VolunteerName
);

public record UpdateVolunteerStatusRequest(
    VolunteerStatus Status
);
