using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Misbahuda.API.Hubs;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Karwan;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class KarwanController(IMediator mediator, IUnitOfWork unitOfWork, IHubContext<TrackingHub> hubContext)
    : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var karwans = await unitOfWork.Karwans.GetAllAsync(cancellationToken);
        var dtos = karwans.Select(k => new KarwanDto(
            k.Id, k.Name, k.PoleNumber, k.IsActive,
            k.CurrentLocation, k.NextStop, k.EstimatedArrival, 0, 0));
        return Ok(ApiResponse<IEnumerable<KarwanDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var karwan = await unitOfWork.Karwans.GetByIdAsync(id, cancellationToken);
        if (karwan is null) return NotFound(ApiResponse<object>.Fail("Karwan not found."));

        var buses = await unitOfWork.Buses.FindAsync(b => b.KarwanId == id, cancellationToken);
        var pilgrims = await unitOfWork.Pilgrims.FindAsync(p => p.KarwanId == id, cancellationToken);

        var dto = new KarwanDto(karwan.Id, karwan.Name, karwan.PoleNumber, karwan.IsActive,
            karwan.CurrentLocation, karwan.NextStop, karwan.EstimatedArrival,
            buses.Count(), pilgrims.Count());

        return Ok(ApiResponse<KarwanDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateKarwanRequest request, CancellationToken cancellationToken)
    {
        var karwan = new Karwan
        {
            Name = request.Name,
            PoleNumber = request.PoleNumber,
            Description = request.Description
        };

        await unitOfWork.Karwans.AddAsync(karwan, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { karwan.Id }, "Karwan created."));
    }

    [HttpPost("gps")]
    [Authorize(Roles = "Driver,Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateGps([FromBody] GpsUpdateRequest request, CancellationToken cancellationToken)
    {
        var location = new GpsLocation
        {
            BusId = request.BusId,
            KarwanId = request.KarwanId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Speed = request.Speed,
            Address = request.Address,
            RecordedAt = DateTime.UtcNow
        };

        await unitOfWork.GpsLocations.AddAsync(location, cancellationToken);

        if (request.KarwanId.HasValue)
        {
            var karwan = await unitOfWork.Karwans.GetByIdAsync(request.KarwanId.Value, cancellationToken);
            if (karwan is not null)
            {
                karwan.CurrentLocation = request.Address ?? $"{request.Latitude},{request.Longitude}";
                unitOfWork.Karwans.Update(karwan);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var locationDto = new GpsLocationDto(request.Latitude, request.Longitude, request.Speed, request.Address, DateTime.UtcNow);

        if (request.KarwanId.HasValue)
            await hubContext.Clients.Group($"karwan-{request.KarwanId}").SendAsync("LocationUpdated", locationDto, cancellationToken);

        if (request.BusId.HasValue)
            await hubContext.Clients.Group($"bus-{request.BusId}").SendAsync("LocationUpdated", locationDto, cancellationToken);

        return Ok(ApiResponse<GpsLocationDto>.Ok(locationDto, "GPS updated."));
    }

    [HttpGet("{id}/location")]
    public async Task<IActionResult> GetLatestLocation(Guid id, CancellationToken cancellationToken)
    {
        var locations = await unitOfWork.GpsLocations.FindAsync(g => g.KarwanId == id, cancellationToken);
        var latest = locations.OrderByDescending(l => l.RecordedAt).FirstOrDefault();

        if (latest is null) return NotFound(ApiResponse<object>.Fail("No GPS data found."));

        return Ok(ApiResponse<GpsLocationDto>.Ok(
            new GpsLocationDto(latest.Latitude, latest.Longitude, latest.Speed, latest.Address, latest.RecordedAt)));
    }

    [HttpPost("{karwanId}/assign-bus/{busId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AssignBus(Guid karwanId, Guid busId, CancellationToken cancellationToken)
    {
        var bus = await unitOfWork.Buses.GetByIdAsync(busId, cancellationToken);
        if (bus is null) return NotFound(ApiResponse<object>.Fail("Bus not found."));

        bus.KarwanId = karwanId;
        unitOfWork.Buses.Update(bus);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Bus assigned to Karwan."));
    }
}
