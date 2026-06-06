using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class MajalisController(IMediator mediator, IUnitOfWork unitOfWork) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var majalis = await unitOfWork.Majalis.FindAsync(m => m.IsActive, cancellationToken);
        return Ok(ApiResponse<IEnumerable<Majalis>>.Ok(majalis.OrderBy(m => m.StartTime)));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMajalisRequest request, CancellationToken cancellationToken)
    {
        var majalis = new Majalis
        {
            Title = request.Title, Language = request.Language,
            MolanaName = request.MolanaName, NohaKhuwanName = request.NohaKhuwanName,
            Venue = request.Venue,
            StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc),
            Description = request.Description,
            ImageUrl = request.ImageUrl
        };
        await unitOfWork.Majalis.AddAsync(majalis, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { majalis.Id }, "Majalis created."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMajalisRequest request, CancellationToken cancellationToken)
    {
        var majalis = await unitOfWork.Majalis.GetByIdAsync(id, cancellationToken);
        if (majalis is null) return NotFound(ApiResponse<object>.Fail("Majalis not found."));
        majalis.Title = request.Title; majalis.Language = request.Language;
        majalis.MolanaName = request.MolanaName; majalis.NohaKhuwanName = request.NohaKhuwanName;
        majalis.Venue = request.Venue;
        majalis.StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        majalis.EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);
        majalis.Description = request.Description;
        majalis.ImageUrl = request.ImageUrl;
        unitOfWork.Majalis.Update(majalis);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var majalis = await unitOfWork.Majalis.GetByIdAsync(id, cancellationToken);
        if (majalis is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        majalis.IsActive = false;
        unitOfWork.Majalis.Update(majalis);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }

    // ── Program Items ────────────────────────────────────────────────────────
    [HttpGet("{id}/program")]
    public async Task<IActionResult> GetProgram(Guid id, CancellationToken cancellationToken)
    {
        var items = await unitOfWork.MajalisPrograms.FindAsync(p => p.MajalisId == id, cancellationToken);
        return Ok(ApiResponse<IEnumerable<MajalisProgram>>.Ok(items.OrderBy(p => p.OrderIndex).ThenBy(p => p.Time)));
    }

    [HttpPost("{id}/program")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SaveProgram(Guid id, [FromBody] List<SaveProgramItemRequest> items, CancellationToken cancellationToken)
    {
        var existing = (await unitOfWork.MajalisPrograms.FindAsync(p => p.MajalisId == id, cancellationToken)).ToList();
        foreach (var ex in existing) unitOfWork.MajalisPrograms.Delete(ex);

        for (int i = 0; i < items.Count; i++)
        {
            var req = items[i];
            await unitOfWork.MajalisPrograms.AddAsync(new MajalisProgram
            {
                MajalisId    = id,
                Type         = req.Type,
                Time         = req.Time,
                PerformerName = req.PerformerName,
                ImageUrl     = req.ImageUrl,
                Description  = req.Description,
                OrderIndex   = i,
            }, cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Program saved."));
    }

    // ── Namaz Timings ────────────────────────────────────────────────────────
    [HttpGet("namaz-timings")]
    public async Task<IActionResult> GetNamazTimings([FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var timings = date.HasValue
            ? await unitOfWork.NamazTimings.FindAsync(n => n.Date.Date == date.Value.Date, cancellationToken)
            : await unitOfWork.NamazTimings.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<NamazTiming>>.Ok(timings.OrderBy(n => n.Time)));
    }

    [HttpPost("namaz-timings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SaveNamazTimings([FromBody] List<SaveNamazTimingRequest> requests, CancellationToken cancellationToken)
    {
        foreach (var req in requests)
        {
            var date = DateTime.SpecifyKind(req.Date.Date, DateTimeKind.Utc);
            var existing = (await unitOfWork.NamazTimings.FindAsync(
                n => n.Date.Date == date.Date && n.PrayerName == req.PrayerName, cancellationToken)).FirstOrDefault();
            if (existing is null)
                await unitOfWork.NamazTimings.AddAsync(new NamazTiming
                    { PrayerName = req.PrayerName, Time = TimeOnly.Parse(req.Time), Date = date, Venue = req.Venue }, cancellationToken);
            else
            { existing.Time = TimeOnly.Parse(req.Time); existing.Venue = req.Venue; unitOfWork.NamazTimings.Update(existing); }
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Namaz timings saved."));
    }

    // ── Food Schedule ────────────────────────────────────────────────────────
    [HttpGet("food-schedule")]
    public async Task<IActionResult> GetFoodSchedule([FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var schedules = date.HasValue
            ? await unitOfWork.FoodSchedules.FindAsync(f => f.ServedAt.Date == date.Value.Date, cancellationToken)
            : await unitOfWork.FoodSchedules.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IEnumerable<FoodSchedule>>.Ok(schedules.OrderBy(f => f.ServedAt)));
    }

    [HttpPost("food-schedule")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateFoodSchedule([FromBody] CreateFoodScheduleRequest request, CancellationToken cancellationToken)
    {
        var food = new FoodSchedule
        {
            MealType = request.MealType,
            ServedAt = DateTime.SpecifyKind(request.ServedAt, DateTimeKind.Utc),
            Location = request.Location, Description = request.Description,
            EstimatedServings = request.EstimatedServings
        };
        await unitOfWork.FoodSchedules.AddAsync(food, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { food.Id }, "Food schedule created."));
    }

    [HttpDelete("food-schedule/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteFood(Guid id, CancellationToken cancellationToken)
    {
        var food = await unitOfWork.FoodSchedules.GetByIdAsync(id, cancellationToken);
        if (food is null) return NotFound();
        unitOfWork.FoodSchedules.Delete(food);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }
}

public record CreateMajalisRequest(
    string Title, string Language,
    string? MolanaName, string? NohaKhuwanName,
    string Venue, DateTime StartTime, DateTime EndTime,
    string? Description, string? ImageUrl);

public record SaveNamazTimingRequest(
    string PrayerName, string Time, DateTime Date, string? Venue);

public record CreateFoodScheduleRequest(
    string MealType, DateTime ServedAt, string Location,
    string? Description, int? EstimatedServings);

public record SaveProgramItemRequest(
    string Type, string Time,
    string? PerformerName, string? ImageUrl, string? Description);
