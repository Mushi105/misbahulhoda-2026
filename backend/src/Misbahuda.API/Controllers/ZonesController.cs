using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class ZonesController(IMediator mediator, IUnitOfWork unitOfWork) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var zones = await unitOfWork.Zones.GetAllAsync(cancellationToken);
        var vols = await unitOfWork.Volunteers.GetAllAsync(cancellationToken);
        var result = zones.Where(z => z.IsActive).OrderBy(z => z.Name).Select(z => new
        {
            z.Id, z.Name, z.ZoneCode, z.Description, z.Location,
            z.MaxVolunteers, z.Latitude, z.Longitude,
            AssignedVolunteers = vols.Count(v => v.ZoneId == z.Id)
        });
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ZoneRequest request, CancellationToken cancellationToken)
    {
        var zone = new Zone
        {
            Name = request.Name, ZoneCode = request.ZoneCode,
            Description = request.Description, Location = request.Location,
            MaxVolunteers = request.MaxVolunteers,
            Latitude = request.Latitude, Longitude = request.Longitude
        };
        await unitOfWork.Zones.AddAsync(zone, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { zone.Id }, "Zone created."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ZoneRequest request, CancellationToken cancellationToken)
    {
        var zone = await unitOfWork.Zones.GetByIdAsync(id, cancellationToken);
        if (zone is null) return NotFound();
        zone.Name = request.Name; zone.ZoneCode = request.ZoneCode;
        zone.Description = request.Description; zone.Location = request.Location;
        zone.MaxVolunteers = request.MaxVolunteers;
        zone.Latitude = request.Latitude; zone.Longitude = request.Longitude;
        unitOfWork.Zones.Update(zone);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Zone updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var zone = await unitOfWork.Zones.GetByIdAsync(id, cancellationToken);
        if (zone is null) return NotFound();
        unitOfWork.Zones.Delete(zone);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Zone deleted."));
    }
}

public record ZoneRequest(
    string Name, string ZoneCode, string Description,
    string? Location, int MaxVolunteers,
    double? Latitude, double? Longitude);
