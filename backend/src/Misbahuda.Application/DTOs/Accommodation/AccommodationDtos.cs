namespace Misbahuda.Application.DTOs.Accommodation;

public record CreateHotelRequest(
    string Name,
    string Address,
    string City,
    string? PhoneNumber,
    string? ContactPerson,
    double? Latitude,
    double? Longitude,
    string? NightsLabel,
    string? NearHaram,
    string? HaramDistanceText,
    double? HaramLatitude,
    double? HaramLongitude,
    string? IconEmoji,
    string? ColorClass,
    string? Amenities,
    string? Tips,
    string? JamatFajr,
    string? AdhanFajr,
    string? JamatZuhr,
    string? AdhanZuhr,
    string? JamatAsr,
    string? AdhanAsr,
    string? JamatMaghrib,
    string? AdhanMaghrib,
    string? JamatIsha,
    string? AdhanIsha
);

public record HotelDto(
    Guid Id,
    string Name,
    string Address,
    string City,
    string? PhoneNumber,
    double? Latitude,
    double? Longitude,
    int TotalBuildings,
    int TotalRooms,
    int AvailableRooms,
    string? NightsLabel,
    string? NearHaram,
    string? HaramDistanceText,
    double? HaramLatitude,
    double? HaramLongitude,
    string? IconEmoji,
    string? ColorClass,
    string? Amenities,
    string? Tips,
    string? JamatFajr,
    string? AdhanFajr,
    string? JamatZuhr,
    string? AdhanZuhr,
    string? JamatAsr,
    string? AdhanAsr,
    string? JamatMaghrib,
    string? AdhanMaghrib,
    string? JamatIsha,
    string? AdhanIsha
);

public record CreateRoomRequest(
    Guid FloorId,
    string RoomNumber,
    int BedCapacity,
    bool IsForFamily
);

public record RoomDto(
    Guid Id,
    string RoomNumber,
    int BedCapacity,
    int OccupiedBeds,
    bool IsForFamily,
    bool IsAvailable,
    string HotelName,
    int FloorNumber
);

public record AddRoomsToHotelRequest(
    int FloorNumber,
    string FloorLabel,
    int TotalRooms,
    int BedCapacity,
    bool IsForFamily
);

public record SimpleHotelRequest(
    string Name,
    string City,
    string? Address,
    string? PhoneNumber
);

public record UpdateFloorRequest(
    int FloorNumber,
    string Label
);
