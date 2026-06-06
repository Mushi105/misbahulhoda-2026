namespace Misbahuda.Application.DTOs.Karwan;

public record CreateKarwanRequest(
    string Name,
    string PoleNumber,
    string? Description
);

public record KarwanDto(
    Guid Id,
    string Name,
    string PoleNumber,
    bool IsActive,
    string? CurrentLocation,
    string? NextStop,
    DateTime? EstimatedArrival,
    int TotalBuses,
    int TotalPilgrims
);

public record GpsUpdateRequest(
    Guid? BusId,
    Guid? KarwanId,
    double Latitude,
    double Longitude,
    double? Speed,
    string? Address
);

public record GpsLocationDto(
    double Latitude,
    double Longitude,
    double? Speed,
    string? Address,
    DateTime RecordedAt
);
