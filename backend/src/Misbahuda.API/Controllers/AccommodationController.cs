using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Accommodation;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class AccommodationController(IMediator mediator, IUnitOfWork unitOfWork) : BaseController(mediator)
{
    [HttpGet("hotels")]
    public async Task<IActionResult> GetHotels(CancellationToken cancellationToken)
    {
        var hotels = await unitOfWork.Hotels.GetAllAsync(cancellationToken);
        var dtos = hotels.Select(h => new HotelDto(
            h.Id, h.Name, h.Address, h.City, h.PhoneNumber,
            h.Latitude, h.Longitude, h.TotalBuildings, 0, 0,
            h.NightsLabel, h.NearHaram, h.HaramDistanceText,
            h.HaramLatitude, h.HaramLongitude,
            h.IconEmoji, h.ColorClass, h.Amenities, h.Tips,
            h.JamatFajr, h.AdhanFajr, h.JamatZuhr, h.AdhanZuhr,
            h.JamatAsr, h.AdhanAsr, h.JamatMaghrib, h.AdhanMaghrib,
            h.JamatIsha, h.AdhanIsha));
        return Ok(ApiResponse<IEnumerable<HotelDto>>.Ok(dtos));
    }

    [HttpPost("hotels")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateHotel([FromBody] CreateHotelRequest request, CancellationToken cancellationToken)
    {
        var hotel = new Hotel
        {
            Name = request.Name, Address = request.Address, City = request.City,
            PhoneNumber = request.PhoneNumber, ContactPerson = request.ContactPerson,
            Latitude = request.Latitude, Longitude = request.Longitude,
            NightsLabel = request.NightsLabel, NearHaram = request.NearHaram,
            HaramDistanceText = request.HaramDistanceText,
            HaramLatitude = request.HaramLatitude, HaramLongitude = request.HaramLongitude,
            IconEmoji = request.IconEmoji, ColorClass = request.ColorClass,
            Amenities = request.Amenities, Tips = request.Tips,
            JamatFajr = request.JamatFajr, AdhanFajr = request.AdhanFajr,
            JamatZuhr = request.JamatZuhr, AdhanZuhr = request.AdhanZuhr,
            JamatAsr = request.JamatAsr, AdhanAsr = request.AdhanAsr,
            JamatMaghrib = request.JamatMaghrib, AdhanMaghrib = request.AdhanMaghrib,
            JamatIsha = request.JamatIsha, AdhanIsha = request.AdhanIsha,
        };

        await unitOfWork.Hotels.AddAsync(hotel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { hotel.Id }, "Hotel created successfully."));
    }

    [HttpPut("hotels/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateHotel(Guid id, [FromBody] CreateHotelRequest request, CancellationToken cancellationToken)
    {
        var hotel = await unitOfWork.Hotels.GetByIdAsync(id, cancellationToken);
        if (hotel is null) return NotFound(ApiResponse<object>.Fail("Hotel not found."));

        hotel.Name = request.Name; hotel.Address = request.Address; hotel.City = request.City;
        hotel.PhoneNumber = request.PhoneNumber; hotel.ContactPerson = request.ContactPerson;
        hotel.Latitude = request.Latitude; hotel.Longitude = request.Longitude;
        hotel.NightsLabel = request.NightsLabel; hotel.NearHaram = request.NearHaram;
        hotel.HaramDistanceText = request.HaramDistanceText;
        hotel.HaramLatitude = request.HaramLatitude; hotel.HaramLongitude = request.HaramLongitude;
        hotel.IconEmoji = request.IconEmoji; hotel.ColorClass = request.ColorClass;
        hotel.Amenities = request.Amenities; hotel.Tips = request.Tips;
        hotel.JamatFajr = request.JamatFajr; hotel.AdhanFajr = request.AdhanFajr;
        hotel.JamatZuhr = request.JamatZuhr; hotel.AdhanZuhr = request.AdhanZuhr;
        hotel.JamatAsr = request.JamatAsr; hotel.AdhanAsr = request.AdhanAsr;
        hotel.JamatMaghrib = request.JamatMaghrib; hotel.AdhanMaghrib = request.AdhanMaghrib;
        hotel.JamatIsha = request.JamatIsha; hotel.AdhanIsha = request.AdhanIsha;

        unitOfWork.Hotels.Update(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { hotel.Id }, "Hotel updated successfully."));
    }

    [HttpGet("rooms")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetRooms([FromQuery] bool? availableOnly, CancellationToken cancellationToken)
    {
        var rooms = availableOnly == true
            ? await unitOfWork.Rooms.FindAsync(r => r.OccupiedBeds < r.BedCapacity, cancellationToken)
            : await unitOfWork.Rooms.GetAllAsync(cancellationToken);
        var roomList = rooms.ToList();

        var floorIds = roomList.Select(r => r.FloorId).Distinct().ToList();
        var floors = floorIds.Any()
            ? await unitOfWork.Floors.FindAsync(f => floorIds.Contains(f.Id), cancellationToken)
            : [];
        var floorMap = floors.ToDictionary(f => f.Id);

        var buildingIds = floors.Select(f => f.BuildingId).Distinct().ToList();
        var buildings = buildingIds.Any()
            ? await unitOfWork.Buildings.FindAsync(b => buildingIds.Contains(b.Id), cancellationToken)
            : [];
        var buildingMap = buildings.ToDictionary(b => b.Id);

        var hotelIds = buildings.Select(b => b.HotelId).Distinct().ToList();
        var hotels = hotelIds.Any()
            ? await unitOfWork.Hotels.FindAsync(h => hotelIds.Contains(h.Id), cancellationToken)
            : [];
        var hotelMap = hotels.ToDictionary(h => h.Id);

        var dtos = roomList.Select(r =>
        {
            floorMap.TryGetValue(r.FloorId, out var floor);
            string hotelName = "";
            int floorNumber = floor?.FloorNumber ?? 0;
            if (floor != null && buildingMap.TryGetValue(floor.BuildingId, out var building)
                && hotelMap.TryGetValue(building.HotelId, out var hotel))
                hotelName = hotel.Name;
            return new RoomDto(r.Id, r.RoomNumber, r.BedCapacity, r.OccupiedBeds,
                r.IsForFamily, r.IsAvailable, hotelName, floorNumber);
        });

        return Ok(ApiResponse<IEnumerable<RoomDto>>.Ok(dtos));
    }

    [HttpPost("rooms")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var floor = await unitOfWork.Floors.GetByIdAsync(request.FloorId, cancellationToken);
        if (floor is null)
            return NotFound(ApiResponse<object>.Fail("Floor not found."));

        var room = new Room
        {
            FloorId = request.FloorId,
            RoomNumber = request.RoomNumber,
            BedCapacity = request.BedCapacity,
            IsForFamily = request.IsForFamily
        };

        await unitOfWork.Rooms.AddAsync(room, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { room.Id }, "Room created successfully."));
    }

    [HttpPost("rooms/{roomId}/allocate/{pilgrimId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AllocateRoom(Guid roomId, Guid pilgrimId, CancellationToken cancellationToken)
    {
        var room = await unitOfWork.Rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found."));
        if (!room.IsAvailable) return BadRequest(ApiResponse<object>.Fail("Room is full."));

        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(pilgrimId, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));

        // Free previous room if switching
        if (pilgrim.RoomId.HasValue && pilgrim.RoomId != roomId)
        {
            var prevRoom = await unitOfWork.Rooms.GetByIdAsync(pilgrim.RoomId.Value, cancellationToken);
            if (prevRoom is not null && prevRoom.OccupiedBeds > 0) { prevRoom.OccupiedBeds--; unitOfWork.Rooms.Update(prevRoom); }
        }

        pilgrim.RoomId = roomId;
        room.OccupiedBeds++;
        unitOfWork.Pilgrims.Update(pilgrim);
        unitOfWork.Rooms.Update(room);

        // Build hotel info for notification message
        string hotelName = "", floorLabel = "", city = "";
        var floor = await unitOfWork.Floors.GetByIdAsync(room.FloorId, cancellationToken);
        if (floor is not null)
        {
            floorLabel = floor.Label ?? $"Floor {floor.FloorNumber}";
            var building = await unitOfWork.Buildings.GetByIdAsync(floor.BuildingId, cancellationToken);
            if (building is not null)
            {
                var hotel = await unitOfWork.Hotels.GetByIdAsync(building.HotelId, cancellationToken);
                if (hotel is not null) { hotelName = hotel.Name; city = hotel.City; }
            }
        }

        // Create history record (check-in)
        var history = new PilgrimRoomHistory
        {
            PilgrimId   = pilgrim.Id,
            RoomId      = room.Id,
            RoomNumber  = room.RoomNumber,
            FloorLabel  = floorLabel,
            HotelName   = hotelName,
            HotelCity   = city,
            CheckedInAt = DateTime.UtcNow
        };
        await unitOfWork.PilgrimRoomHistories.AddAsync(history, cancellationToken);

        var notification = new Notification
        {
            UserId = pilgrim.UserId,
            Title = "🏨 Room Assigned",
            Message = $"You have been assigned Room {room.RoomNumber} ({floorLabel}) at {hotelName}, {city}. Please check your portal for hotel details and directions.",
            Type = NotificationType.Push,
            Event = NotificationEvent.RoomAllocation
        };
        await unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Room allocated successfully."));
    }

    [HttpGet("rooms/detail")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetRoomsWithOccupants(CancellationToken cancellationToken)
    {
        var rooms = await unitOfWork.Rooms.GetAllAsync(cancellationToken);
        var roomList = rooms.ToList();

        // Load full hierarchy
        var floorIds = roomList.Select(r => r.FloorId).Distinct().ToList();
        var floors = floorIds.Any() ? await unitOfWork.Floors.FindAsync(f => floorIds.Contains(f.Id), cancellationToken) : [];
        var floorMap = floors.ToDictionary(f => f.Id);

        var buildingIds = floors.Select(f => f.BuildingId).Distinct().ToList();
        var buildings = buildingIds.Any() ? await unitOfWork.Buildings.FindAsync(b => buildingIds.Contains(b.Id), cancellationToken) : [];
        var buildingMap = buildings.ToDictionary(b => b.Id);

        var hotelIds = buildings.Select(b => b.HotelId).Distinct().ToList();
        var hotels = hotelIds.Any() ? await unitOfWork.Hotels.FindAsync(h => hotelIds.Contains(h.Id), cancellationToken) : [];
        var hotelMap = hotels.ToDictionary(h => h.Id);

        // Load pilgrims assigned to rooms
        var roomIds = roomList.Where(r => r.OccupiedBeds > 0).Select(r => r.Id).ToList();
        var pilgrims = roomIds.Any()
            ? await unitOfWork.Pilgrims.FindAsync(p => p.RoomId.HasValue && roomIds.Contains(p.RoomId.Value), cancellationToken)
            : [];
        var pilgrimList = pilgrims.ToList();

        var userIds = pilgrimList.Select(p => p.UserId).Distinct().ToList();
        var users = userIds.Any() ? await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken) : [];
        var userMap = users.ToDictionary(u => u.Id);

        var pilgrimsByRoom = pilgrimList.GroupBy(p => p.RoomId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var result = roomList.Select(r =>
        {
            floorMap.TryGetValue(r.FloorId, out var floor);
            var floorNumber = floor?.FloorNumber ?? 0;
            var floorLabel = floor?.Label ?? $"Floor {floorNumber}";
            string hotelName = "", buildingName = "";
            Guid hotelId = Guid.Empty;

            if (floor != null && buildingMap.TryGetValue(floor.BuildingId, out var building))
            {
                buildingName = building.Name;
                if (hotelMap.TryGetValue(building.HotelId, out var hotel))
                {
                    hotelName = hotel.Name;
                    hotelId = hotel.Id;
                }
            }

            pilgrimsByRoom.TryGetValue(r.Id, out var roomPilgrims);
            var occupants = (roomPilgrims ?? []).Select(p =>
            {
                userMap.TryGetValue(p.UserId, out var user);
                return new
                {
                    PilgrimId = p.Id,
                    UserId = p.UserId,
                    FullName = user?.FullName ?? "Unknown",
                    Phone = user?.PhoneNumber ?? "",
                    Country = p.Country,
                    FamilyMemberCount = p.FamilyMemberCount,
                    CheckIn = p.ArrivalDate,
                    CheckOut = p.DepartureDate,
                    Status = p.Status.ToString()
                };
            }).ToList();

            return new
            {
                r.Id,
                r.RoomNumber,
                r.BedCapacity,
                r.OccupiedBeds,
                r.IsForFamily,
                IsAvailable = r.IsAvailable,
                FloorId = r.FloorId,
                FloorNumber = floorNumber,
                FloorLabel = floorLabel,
                HotelId = hotelId,
                HotelName = hotelName,
                BuildingName = buildingName,
                Occupants = occupants
            };
        }).OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomNumber).ToList();

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpDelete("rooms/{roomId}/deallocate/{pilgrimId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeallocateRoom(Guid roomId, Guid pilgrimId, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(pilgrimId, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));

        var room = await unitOfWork.Rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null) return NotFound(ApiResponse<object>.Fail("Room not found."));

        pilgrim.RoomId = null;
        if (room.OccupiedBeds > 0) room.OccupiedBeds--;
        unitOfWork.Pilgrims.Update(pilgrim);
        unitOfWork.Rooms.Update(room);

        // Close history record
        var openHistory = (await unitOfWork.PilgrimRoomHistories.FindAsync(
            h => h.PilgrimId == pilgrim.Id && h.RoomId == roomId && h.CheckedOutAt == null, cancellationToken))
            .FirstOrDefault();
        if (openHistory is not null)
        {
            openHistory.CheckedOutAt = DateTime.UtcNow;
            openHistory.CheckedOutBy = "Admin";
            unitOfWork.PilgrimRoomHistories.Update(openHistory);
        }

        var notification = new Notification
        {
            UserId = pilgrim.UserId,
            Title = "🔄 Room Unassigned",
            Message = $"Your room assignment (Room {room.RoomNumber}) has been removed by the team. A new room will be assigned shortly.",
            Type = NotificationType.Push,
            Event = NotificationEvent.RoomAllocation
        };
        await unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Pilgrim removed from room."));
    }

    [HttpGet("pilgrim-trail/{pilgrimId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetPilgrimTrail(Guid pilgrimId, CancellationToken cancellationToken)
    {
        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(pilgrimId, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));

        var user = await unitOfWork.Users.GetByIdAsync(pilgrim.UserId, cancellationToken);
        var history = await unitOfWork.PilgrimRoomHistories.FindAsync(h => h.PilgrimId == pilgrimId, cancellationToken);

        var trail = history.OrderBy(h => h.CheckedInAt).Select(h => new
        {
            h.Id,
            h.HotelName,
            h.HotelCity,
            h.RoomNumber,
            h.FloorLabel,
            h.CheckedInAt,
            h.CheckedOutAt,
            h.CheckedOutBy,
            IsCurrentStay = h.CheckedOutAt == null
        }).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            PilgrimId = pilgrim.Id,
            PilgrimName = user?.FullName ?? "Unknown",
            Country = pilgrim.Country,
            CurrentRoomId = pilgrim.RoomId,
            Trail = trail
        }));
    }

    [HttpGet("in-transit")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetInTransitPilgrims(CancellationToken cancellationToken)
    {
        // Pilgrims who have checked out (have history with CheckedOutAt) but currently have no room
        var pilgrims = await unitOfWork.Pilgrims.FindAsync(p => !p.RoomId.HasValue, cancellationToken);
        var pilgrimList = pilgrims.ToList();

        if (!pilgrimList.Any())
            return Ok(ApiResponse<object>.Ok(new List<object>()));

        var pilgrimIds = pilgrimList.Select(p => p.Id).ToList();
        var allHistory = await unitOfWork.PilgrimRoomHistories.FindAsync(
            h => pilgrimIds.Contains(h.PilgrimId), cancellationToken);

        // Only include pilgrims who have at least one history record (previously stayed somewhere)
        var pilgrimsWithHistory = new HashSet<Guid>(allHistory.Select(h => h.PilgrimId));
        var inTransit = pilgrimList.Where(p => pilgrimsWithHistory.Contains(p.Id)).ToList();

        if (!inTransit.Any())
            return Ok(ApiResponse<object>.Ok(new List<object>()));

        var userIds = inTransit.Select(p => p.UserId).Distinct().ToList();
        var users = await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var historyByPilgrim = allHistory.GroupBy(h => h.PilgrimId).ToDictionary(g => g.Key, g => g.ToList());

        var result = inTransit.Select(p =>
        {
            userMap.TryGetValue(p.UserId, out var user);
            historyByPilgrim.TryGetValue(p.Id, out var pHistory);
            var lastStay = pHistory?.OrderByDescending(h => h.CheckedOutAt ?? h.CheckedInAt).FirstOrDefault();

            return new
            {
                PilgrimId = p.Id,
                FullName = user?.FullName ?? "Unknown",
                Phone = user?.PhoneNumber ?? "",
                Country = p.Country,
                LastHotel = lastStay?.HotelName ?? "",
                LastCity = lastStay?.HotelCity ?? "",
                LastRoom = lastStay?.RoomNumber ?? "",
                CheckedOutAt = lastStay?.CheckedOutAt,
                CheckedOutBy = lastStay?.CheckedOutBy ?? "",
                StayCount = pHistory?.Count ?? 0
            };
        }).OrderByDescending(p => p.CheckedOutAt).ToList();

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("hotels/simple")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateSimpleHotel([FromBody] SimpleHotelRequest request, CancellationToken cancellationToken)
    {
        var hotel = new Hotel
        {
            Name = request.Name,
            City = request.City,
            Address = request.Address ?? "",
            PhoneNumber = request.PhoneNumber,
        };
        await unitOfWork.Hotels.AddAsync(hotel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { hotel.Id, hotel.Name, hotel.City }, "Hotel created."));
    }

    [HttpGet("hotels/with-rooms")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetHotelsWithRooms(CancellationToken cancellationToken)
    {
        var hotels   = (await unitOfWork.Hotels.GetAllAsync(cancellationToken)).ToList();
        var buildings = (await unitOfWork.Buildings.GetAllAsync(cancellationToken)).ToList();
        var floors    = (await unitOfWork.Floors.GetAllAsync(cancellationToken)).ToList();
        var rooms     = (await unitOfWork.Rooms.GetAllAsync(cancellationToken)).ToList();

        var buildingMap = buildings.GroupBy(b => b.HotelId).ToDictionary(g => g.Key, g => g.ToList());
        var floorMap    = floors.GroupBy(f => f.BuildingId).ToDictionary(g => g.Key, g => g.ToList());
        var roomMap     = rooms.GroupBy(r => r.FloorId).ToDictionary(g => g.Key, g => g.ToList());

        var result = hotels.Select(h =>
        {
            var hBuildings = buildingMap.GetValueOrDefault(h.Id, []);
            var hFloors    = hBuildings.SelectMany(b => floorMap.GetValueOrDefault(b.Id, [])).ToList();
            var hRooms     = hFloors.SelectMany(f => roomMap.GetValueOrDefault(f.Id, [])).ToList();

            var floorDetails = hFloors.Select(f =>
            {
                var fRooms = roomMap.GetValueOrDefault(f.Id, []);
                return new
                {
                    f.Id, f.FloorNumber, Label = f.Label ?? $"Floor {f.FloorNumber}",
                    TotalRooms = fRooms.Count,
                    AvailableRooms = fRooms.Count(r => r.IsAvailable),
                    OccupiedBeds = fRooms.Sum(r => r.OccupiedBeds),
                    TotalBeds = fRooms.Sum(r => r.BedCapacity)
                };
            }).OrderBy(f => f.FloorNumber).ToList();

            return new
            {
                h.Id, h.Name, h.City, h.Address, h.PhoneNumber,
                TotalRooms = hRooms.Count,
                AvailableRooms = hRooms.Count(r => r.IsAvailable),
                TotalBeds = hRooms.Sum(r => r.BedCapacity),
                OccupiedBeds = hRooms.Sum(r => r.OccupiedBeds),
                Floors = floorDetails
            };
        }).ToList();

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("hotels/{hotelId}/add-rooms")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddRoomsToHotel(Guid hotelId, [FromBody] AddRoomsToHotelRequest request, CancellationToken cancellationToken)
    {
        var hotel = await unitOfWork.Hotels.GetByIdAsync(hotelId, cancellationToken);
        if (hotel is null) return NotFound(ApiResponse<object>.Fail("Hotel not found."));

        if (request.TotalRooms < 1 || request.TotalRooms > 500)
            return BadRequest(ApiResponse<object>.Fail("Total rooms must be between 1 and 500."));

        // Find or create default building
        var buildings = await unitOfWork.Buildings.FindAsync(b => b.HotelId == hotelId, cancellationToken);
        var building = buildings.FirstOrDefault();
        if (building is null)
        {
            building = new Building { HotelId = hotelId, Name = "Main Building", TotalFloors = 0 };
            await unitOfWork.Buildings.AddAsync(building, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Find or create floor
        var existingFloors = await unitOfWork.Floors.FindAsync(f => f.BuildingId == building.Id && f.FloorNumber == request.FloorNumber, cancellationToken);
        var floor = existingFloors.FirstOrDefault();
        if (floor is null)
        {
            floor = new Floor { BuildingId = building.Id, FloorNumber = request.FloorNumber, Label = request.FloorLabel };
            await unitOfWork.Floors.AddAsync(floor, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Bulk create rooms — standard numbering: Floor 5, room 1 = "501", room 2 = "502"
        var existingRooms = await unitOfWork.Rooms.FindAsync(r => r.FloorId == floor.Id, cancellationToken);
        var existingNumbers = new HashSet<string>(existingRooms.Select(r => r.RoomNumber));

        int created = 0;
        for (int i = 1; i <= request.TotalRooms; i++)
        {
            var num = $"{request.FloorNumber}{i:D2}";  // Floor 5, i=1 → "501"; i=10 → "510"
            if (existingNumbers.Contains(num)) continue;
            await unitOfWork.Rooms.AddAsync(new Room
            {
                FloorId = floor.Id,
                RoomNumber = num,
                BedCapacity = request.BedCapacity,
                IsForFamily = request.IsForFamily
            }, cancellationToken);
            created++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { Created = created }, $"{created} rooms added to {hotel.Name}."));
    }

    [HttpPut("floors/{floorId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateFloor(Guid floorId, [FromBody] UpdateFloorRequest request, CancellationToken cancellationToken)
    {
        var floor = await unitOfWork.Floors.GetByIdAsync(floorId, cancellationToken);
        if (floor is null) return NotFound(ApiResponse<object>.Fail("Floor not found."));

        int oldFloorNumber = floor.FloorNumber;
        floor.FloorNumber = request.FloorNumber;
        floor.Label = request.Label;
        unitOfWork.Floors.Update(floor);

        // Rename rooms if floor number changed — "601" → "201" etc.
        if (oldFloorNumber != request.FloorNumber)
        {
            var rooms = await unitOfWork.Rooms.FindAsync(r => r.FloorId == floorId, cancellationToken);
            var prefix = oldFloorNumber.ToString();
            foreach (var room in rooms)
            {
                if (room.RoomNumber.StartsWith(prefix) && room.RoomNumber.Length > prefix.Length
                    && int.TryParse(room.RoomNumber[prefix.Length..], out int seq))
                {
                    room.RoomNumber = $"{request.FloorNumber}{seq:D2}";
                    unitOfWork.Rooms.Update(room);
                }
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Floor updated. Room numbers renamed."));
    }

    [HttpDelete("floors/{floorId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteFloor(Guid floorId, CancellationToken cancellationToken)
    {
        var floor = await unitOfWork.Floors.GetByIdAsync(floorId, cancellationToken);
        if (floor is null) return NotFound(ApiResponse<object>.Fail("Floor not found."));

        var rooms = await unitOfWork.Rooms.FindAsync(r => r.FloorId == floorId, cancellationToken);
        if (rooms.Any(r => r.OccupiedBeds > 0))
            return BadRequest(ApiResponse<object>.Fail("Cannot delete floor — some rooms have pilgrims assigned. Please deallocate first."));

        foreach (var room in rooms) unitOfWork.Rooms.Delete(room);
        unitOfWork.Floors.Delete(floor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Floor and its rooms deleted."));
    }

    [HttpDelete("hotels/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteHotel(Guid id, CancellationToken cancellationToken)
    {
        var hotel = await unitOfWork.Hotels.GetByIdAsync(id, cancellationToken);
        if (hotel is null) return NotFound(ApiResponse<object>.Fail("Hotel not found."));
        unitOfWork.Hotels.Delete(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Hotel deleted."));
    }

    [HttpGet("occupancy")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetOccupancy(CancellationToken cancellationToken)
    {
        var rooms = await unitOfWork.Rooms.GetAllAsync(cancellationToken);
        var totalBeds = rooms.Sum(r => r.BedCapacity);
        var occupiedBeds = rooms.Sum(r => r.OccupiedBeds);

        return Ok(ApiResponse<object>.Ok(new
        {
            TotalRooms = rooms.Count(),
            AvailableRooms = rooms.Count(r => r.IsAvailable),
            TotalBeds = totalBeds,
            OccupiedBeds = occupiedBeds,
            OccupancyRate = totalBeds > 0 ? Math.Round((double)occupiedBeds / totalBeds * 100, 1) : 0
        }));
    }
}
