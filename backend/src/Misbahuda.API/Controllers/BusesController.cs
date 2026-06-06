using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class BusesController(
    IMediator mediator,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var buses = await unitOfWork.Buses.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<Bus>>.Ok(buses));
    }

    /// <summary>Returns buses assigned to the currently logged-in driver.</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null) return Unauthorized();

        var buses = await unitOfWork.Buses.FindAsync(
            b => b.DriverUserId == currentUser.UserId, cancellationToken);

        return Ok(ApiResponse<IEnumerable<Bus>>.Ok(buses));
    }

    /// <summary>Returns pilgrims assigned to a specific bus.</summary>
    [HttpGet("{busId}/passengers")]
    public async Task<IActionResult> GetPassengers(Guid busId, CancellationToken cancellationToken)
    {
        var pilgrims = await unitOfWork.Pilgrims.FindAsync(
            p => p.BusId == busId, cancellationToken);

        var userIds = pilgrims.Select(p => p.UserId).ToList();
        var users   = await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var result = pilgrims.Select(p => new {
            p.Id,
            FullName = userMap.TryGetValue(p.UserId, out var u) ? u.FullName : "Unknown",
            p.Country,
            SeatNumber = (string?)null,
            IsBoarded  = false
        });

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateBusRequest request, CancellationToken cancellationToken)
    {
        var bus = new Bus
        {
            BusNumber = request.BusNumber,
            PlateNumber = request.PlateNumber,
            Capacity = request.Capacity,
            DriverName = request.DriverName,
            DriverPhone = request.DriverPhone
        };

        await unitOfWork.Buses.AddAsync(bus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { bus.Id }, "Bus created."));
    }

    [HttpPost("{busId}/allocate/{pilgrimId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AllocatePilgrim(Guid busId, Guid pilgrimId, CancellationToken cancellationToken)
    {
        var bus = await unitOfWork.Buses.GetByIdAsync(busId, cancellationToken);
        if (bus is null) return NotFound(ApiResponse<object>.Fail("Bus not found."));
        if (bus.CurrentPassengers >= bus.Capacity)
            return BadRequest(ApiResponse<object>.Fail("Bus is full."));

        var pilgrim = await unitOfWork.Pilgrims.GetByIdAsync(pilgrimId, cancellationToken);
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("Pilgrim not found."));

        pilgrim.BusId = busId;
        bus.CurrentPassengers++;

        unitOfWork.Pilgrims.Update(pilgrim);
        unitOfWork.Buses.Update(bus);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Pilgrim allocated to bus."));
    }
}

public record CreateBusRequest(
    string BusNumber,
    string PlateNumber,
    int Capacity,
    string? DriverName,
    string? DriverPhone
);
