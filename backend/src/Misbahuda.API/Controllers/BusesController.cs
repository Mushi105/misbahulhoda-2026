using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class BusesController(IMediator mediator, IUnitOfWork unitOfWork) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var buses = await unitOfWork.Buses.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<Bus>>.Ok(buses));
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
