using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Enums;
using Misbahuda.Domain.Interfaces;
using Misbahuda.Infrastructure.Services;

namespace Misbahuda.API.Controllers;

public class ToursController(IMediator mediator, IUnitOfWork uow, ICurrentUserService currentUser,
    IEmailService emailService, IWhatsAppService whatsApp)
    : BaseController(mediator)
{
    // ── Public: list open tours (for pilgrim apply page) ─────────────────────
    [HttpGet("open")]
    [Authorize]
    public async Task<IActionResult> GetOpenTours(CancellationToken ct)
    {
        var tours = (await uow.Tours.FindAsync(t => t.IsOpen && t.Status != TourStatus.Cancelled, ct)).ToList();
        var packages = tours.Any()
            ? (await uow.TourPackages.FindAsync(p => tours.Select(t => t.Id).Contains(p.TourId) && p.IsActive, ct)).ToList()
            : [];

        return Ok(ApiResponse<object>.Ok(tours.Select(t => new
        {
            t.Id, t.TourName, t.TourType, t.Year, t.StartDate, t.EndDate, t.Status, t.IsOpen, t.Description,
            Packages = packages.Where(p => p.TourId == t.Id).Select(p => new
            {
                p.Id, p.PackageName, p.Description, p.Price, p.Currency, p.MaxPilgrims,
                p.DurationDays, p.TripDuration, p.Destinations, p.RouteDetail,
                p.IncludesAirportTransfer, p.IncludesBreakfast,
                p.IncludesLunch, p.IncludesDinner, p.HotelInfo, p.Notes,
                CurrentPilgrims = 0
            })
        })));
    }

    // ── Public: get single tour ───────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetTour(Guid id, CancellationToken ct)
    {
        var tour = await uow.Tours.GetByIdAsync(id, ct);
        if (tour is null) return NotFound(ApiResponse<object>.Fail("Tour not found."));
        var packages = (await uow.TourPackages.FindAsync(p => p.TourId == id && p.IsActive, ct)).ToList();
        return Ok(ApiResponse<object>.Ok(new { tour, packages }));
    }

    // ── Admin: list all tours ─────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tours    = (await uow.Tours.GetAllAsync(ct)).OrderByDescending(t => t.Year).ThenBy(t => t.TourType).ToList();
        var packages = (await uow.TourPackages.GetAllAsync(ct)).ToList();
        var pilgrims = (await uow.Pilgrims.GetAllAsync(ct)).ToList();

        return Ok(ApiResponse<object>.Ok(tours.Select(t => new
        {
            t.Id, t.TourName, t.TourType, t.Year, t.StartDate, t.EndDate,
            t.Status, t.IsOpen, t.Description,
            TotalPilgrims    = pilgrims.Count(p => p.TourId == t.Id),
            ApprovedPilgrims = pilgrims.Count(p => p.TourId == t.Id && p.Status == ApplicationStatus.Approved),
            Packages = packages.Where(p => p.TourId == t.Id).Select(p => new
            {
                p.Id, p.PackageName, p.Description, p.Price, p.Currency, p.MaxPilgrims, p.IsActive,
                p.DurationDays, p.TripDuration, p.Destinations, p.RouteDetail,
                p.IncludesAirportTransfer, p.IncludesBreakfast,
                p.IncludesLunch, p.IncludesDinner, p.HotelInfo, p.Notes,
                PilgrimsCount = pilgrims.Count(x => x.PackageId == p.Id)
            })
        })));
    }

    // ── Admin: create tour ────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] TourRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.TourName))
            return BadRequest(ApiResponse<object>.Fail("Tour name is required."));

        var duplicate = (await uow.Tours.FindAsync(
            t => t.TourName == req.TourName.Trim() && t.Year == req.Year, ct)).FirstOrDefault();
        if (duplicate is not null)
            return Conflict(ApiResponse<object>.Fail($"A tour named \"{req.TourName}\" already exists for {req.Year}. Please edit the existing tour instead."));

        var tour = new Tour
        {
            TourName    = req.TourName,
            TourType    = (TourType)req.TourType,
            Year        = req.Year,
            StartDate   = req.StartDate ?? DateTime.UtcNow,
            EndDate     = req.EndDate   ?? DateTime.UtcNow.AddDays(14),
            Status      = TourStatus.Upcoming,
            IsOpen      = req.IsOpen,
            Description = req.Description,
        };
        await uow.Tours.AddAsync(tour, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { tour.Id }, "Tour created."));
    }

    // ── Admin: update tour ────────────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TourRequest req, CancellationToken ct)
    {
        var tour = await uow.Tours.GetByIdAsync(id, ct);
        if (tour is null) return NotFound(ApiResponse<object>.Fail("Tour not found."));

        var duplicate = (await uow.Tours.FindAsync(
            t => t.Id != id && t.TourName == req.TourName.Trim() && t.Year == req.Year, ct)).FirstOrDefault();
        if (duplicate is not null)
            return Conflict(ApiResponse<object>.Fail($"Another tour named \"{req.TourName}\" already exists for {req.Year}."));

        tour.TourName    = req.TourName.Trim();
        tour.TourType    = (TourType)req.TourType;
        tour.Year        = req.Year;
        if (req.StartDate.HasValue) tour.StartDate = req.StartDate.Value;
        if (req.EndDate.HasValue)   tour.EndDate   = req.EndDate.Value;
        tour.IsOpen      = req.IsOpen;
        tour.Description = req.Description;
        if (req.Status.HasValue) tour.Status = (TourStatus)req.Status.Value;
        uow.Tours.Update(tour);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Updated."));
    }

    // ── Admin: notify all existing customers about a tour ────────────────────
    [HttpPost("{id:guid}/notify-customers")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> NotifyCustomers(Guid id, CancellationToken ct)
    {
        var tour = await uow.Tours.GetByIdAsync(id, ct);
        if (tour is null) return NotFound(ApiResponse<object>.Fail("Tour not found."));

        var packages = (await uow.TourPackages.FindAsync(p => p.TourId == id && p.IsActive, ct)).ToList();

        // All users who have ever been a pilgrim (distinct)
        var allPilgrims = (await uow.Pilgrims.GetAllAsync(ct)).ToList();
        var userIds = allPilgrims.Select(p => p.UserId).Distinct().ToList();
        var users = (await uow.Users.FindAsync(u => userIds.Contains(u.Id) && u.IsActive, ct)).ToList();

        var sent = 0;
        foreach (var user in users)
        {
            var html  = BuildNewTourEmail(user.FullName, tour, packages);
            var waMsg = BuildNewTourWhatsApp(user.FullName, tour, packages);

            try
            {
                await emailService.SendAsync(user.Email, user.FullName,
                    $"🕌 New Tour Announced — {tour.TourName} | Misbah ul Hoda", html);

                var phone = user.WhatsAppNumber ?? user.PhoneNumber;
                if (!string.IsNullOrEmpty(phone))
                    await whatsApp.SendMessageAsync(phone, waMsg);

                sent++;
            }
            catch { /* skip failed sends */ }
        }

        return Ok(ApiResponse<object>.Ok(new { Sent = sent }, $"Notification sent to {sent} customers."));
    }

    // ── Admin: mark tour completed + send feedback emails ────────────────────
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CompleteTour(Guid id, CancellationToken ct)
    {
        var tour = await uow.Tours.GetByIdAsync(id, ct);
        if (tour is null) return NotFound(ApiResponse<object>.Fail("Tour not found."));

        tour.Status = TourStatus.Completed;
        tour.IsOpen = false;
        uow.Tours.Update(tour);
        await uow.SaveChangesAsync(ct);

        // Fire feedback emails in background
        _ = Task.Run(() => SendFeedbackEmailsAsync(id, tour.TourName, CancellationToken.None));

        return Ok(ApiResponse<object>.Ok(new { }, "Tour marked as completed. Feedback emails are being sent."));
    }

    // ── Admin: send feedback emails manually ─────────────────────────────────
    [HttpPost("{id:guid}/send-feedback-emails")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SendFeedbackEmails(Guid id, CancellationToken ct)
    {
        var tour = await uow.Tours.GetByIdAsync(id, ct);
        if (tour is null) return NotFound(ApiResponse<object>.Fail("Tour not found."));
        _ = Task.Run(() => SendFeedbackEmailsAsync(id, tour.TourName, CancellationToken.None));
        return Ok(ApiResponse<object>.Ok(new { }, "Feedback emails are being sent."));
    }

    // ── Package CRUD ──────────────────────────────────────────────────────────
    [HttpPost("{tourId:guid}/packages")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddPackage(Guid tourId, [FromBody] PackageRequest req, CancellationToken ct)
    {
        var pkg = new TourPackage
        {
            TourId = tourId, PackageName = req.PackageName, Description = req.Description,
            Price = req.Price, Currency = req.Currency, MaxPilgrims = req.MaxPilgrims,
            DurationDays = req.DurationDays,
            TripDuration = (TripDuration)req.TripDuration,
            Destinations = req.Destinations ?? string.Empty,
            RouteDetail  = req.RouteDetail,
            IncludesAirportTransfer = req.IncludesAirportTransfer,
            IncludesBreakfast = req.IncludesBreakfast, IncludesLunch = req.IncludesLunch,
            IncludesDinner = req.IncludesDinner, HotelInfo = req.HotelInfo, Notes = req.Notes,
        };
        await uow.TourPackages.AddAsync(pkg, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { pkg.Id }, "Package added."));
    }

    [HttpPut("packages/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdatePackage(Guid id, [FromBody] PackageRequest req, CancellationToken ct)
    {
        var pkg = await uow.TourPackages.GetByIdAsync(id, ct);
        if (pkg is null) return NotFound(ApiResponse<object>.Fail("Package not found."));
        pkg.PackageName = req.PackageName; pkg.Description = req.Description;
        pkg.Price = req.Price; pkg.Currency = req.Currency; pkg.MaxPilgrims = req.MaxPilgrims;
        pkg.DurationDays = req.DurationDays;
        pkg.TripDuration = (TripDuration)req.TripDuration;
        pkg.Destinations = req.Destinations ?? string.Empty;
        pkg.RouteDetail  = req.RouteDetail;
        pkg.IncludesAirportTransfer = req.IncludesAirportTransfer;
        pkg.IncludesBreakfast = req.IncludesBreakfast; pkg.IncludesLunch = req.IncludesLunch;
        pkg.IncludesDinner = req.IncludesDinner; pkg.HotelInfo = req.HotelInfo;
        pkg.Notes = req.Notes; pkg.IsActive = req.IsActive;
        uow.TourPackages.Update(pkg);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Package updated."));
    }

    [HttpDelete("packages/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeletePackage(Guid id, CancellationToken ct)
    {
        var pkg = await uow.TourPackages.GetByIdAsync(id, ct);
        if (pkg is null) return NotFound(ApiResponse<object>.Fail("Package not found."));
        uow.TourPackages.Delete(pkg);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }

    // ── Pilgrim: apply for a tour/package ────────────────────────────────────
    [HttpPost("{tourId:guid}/apply")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> Apply(Guid tourId, [FromBody] TourApplyRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var tour = await uow.Tours.GetByIdAsync(tourId, ct);
        if (tour is null || !tour.IsOpen) return BadRequest(ApiResponse<object>.Fail("This tour is not accepting applications."));

        // Check if already applied
        var existing = (await uow.Pilgrims.FindAsync(p => p.UserId == userId && p.TourId == tourId, ct)).FirstOrDefault();
        if (existing is not null) return BadRequest(ApiResponse<object>.Fail("You have already applied for this tour."));

        // Get previous application to pre-fill data
        var previous = (await uow.Pilgrims.FindAsync(p => p.UserId == userId, ct))
            .OrderByDescending(p => p.CreatedAt).FirstOrDefault();

        var pilgrim = new Pilgrim
        {
            UserId           = userId.Value,
            TourId           = tourId,
            PackageId        = req.PackageId,
            Status           = ApplicationStatus.Pending,
            // New fields from request
            PassportNumber   = req.PassportNumber ?? previous?.PassportNumber,
            PassportExpiry   = req.PassportExpiry ?? previous?.PassportExpiry,
            VisaNumber       = req.VisaNumber,
            Country          = req.Country ?? previous?.Country ?? string.Empty,
            FamilyMemberCount = req.FamilyMemberCount,
            ArrivalDate      = req.ArrivalDate,
            DepartureDate    = req.DepartureDate,
            ArrivalFlight    = req.ArrivalFlight,
            DepartureFlight  = req.DepartureFlight,
            ArrivalAirport   = req.ArrivalAirport,
            DepartureAirport = req.DepartureAirport,
            ArrivalTime      = req.ArrivalTime,
            DepartureTime    = req.DepartureTime,
            EmergencyContactName  = req.EmergencyContactName  ?? previous?.EmergencyContactName,
            EmergencyContactPhone = req.EmergencyContactPhone ?? previous?.EmergencyContactPhone,
        };
        await uow.Pilgrims.AddAsync(pilgrim, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { pilgrim.Id }, "Application submitted successfully."));
    }

    // ── Pilgrim: my application history across all tours ─────────────────────
    [HttpGet("my-history")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> GetMyHistory(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var applications = (await uow.Pilgrims.FindAsync(p => p.UserId == userId, ct))
            .OrderByDescending(p => p.CreatedAt).ToList();

        if (!applications.Any()) return Ok(ApiResponse<object>.Ok(Array.Empty<object>()));

        var tourIds    = applications.Where(p => p.TourId.HasValue).Select(p => p.TourId!.Value).Distinct().ToList();
        var packageIds = applications.Where(p => p.PackageId.HasValue).Select(p => p.PackageId!.Value).Distinct().ToList();

        var tours    = tourIds.Any()    ? (await uow.Tours.FindAsync(t => tourIds.Contains(t.Id), ct)).ToDictionary(t => t.Id) : new();
        var packages = packageIds.Any() ? (await uow.TourPackages.FindAsync(p => packageIds.Contains(p.Id), ct)).ToDictionary(p => p.Id) : new();
        var feedbacks = (await uow.PilgrimFeedbacks.FindAsync(f => applications.Select(a => a.Id).Contains(f.PilgrimId), ct)).ToDictionary(f => f.PilgrimId);

        return Ok(ApiResponse<object>.Ok(applications.Select(p =>
        {
            tours.TryGetValue(p.TourId ?? Guid.Empty, out var tour);
            packages.TryGetValue(p.PackageId ?? Guid.Empty, out var pkg);
            feedbacks.TryGetValue(p.Id, out var feedback);
            return new
            {
                p.Id, p.Status, p.CreatedAt,
                p.ArrivalDate, p.DepartureDate,
                p.ArrivalFlight, p.DepartureFlight,
                p.ArrivalAirport, p.DepartureAirport,
                p.FamilyMemberCount,
                Tour    = tour    is null ? null : new { tour.Id, tour.TourName, tour.TourType, tour.Year, tour.Status },
                Package = pkg     is null ? null : new { pkg.Id, pkg.PackageName, pkg.Price, pkg.Currency, pkg.HotelInfo },
                Feedback = feedback is null ? null : new
                {
                    feedback.OverallRating, feedback.HotelRating, feedback.TransportRating,
                    feedback.OrganizationRating, feedback.FoodRating,
                    feedback.Comment, feedback.Suggestions,
                    feedback.WouldRecommend, feedback.WouldReturn,
                    feedback.PreferredPackageNextYear, feedback.SubmittedAt,
                },
                FeedbackPending = p.FeedbackSent && feedback is null,
            };
        })));
    }

    // ── Admin: user history ───────────────────────────────────────────────────
    [HttpGet("user-history/{userId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetUserHistory(Guid userId, CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(userId, ct);
        if (user is null) return NotFound(ApiResponse<object>.Fail("User not found."));

        var applications = (await uow.Pilgrims.FindAsync(p => p.UserId == userId, ct))
            .OrderByDescending(p => p.CreatedAt).ToList();

        var tourIds    = applications.Where(p => p.TourId.HasValue).Select(p => p.TourId!.Value).Distinct().ToList();
        var packageIds = applications.Where(p => p.PackageId.HasValue).Select(p => p.PackageId!.Value).Distinct().ToList();
        var tours    = tourIds.Any()    ? (await uow.Tours.FindAsync(t => tourIds.Contains(t.Id), ct)).ToDictionary(t => t.Id) : new();
        var packages = packageIds.Any() ? (await uow.TourPackages.FindAsync(p => packageIds.Contains(p.Id), ct)).ToDictionary(p => p.Id) : new();
        var feedbacks = applications.Any()
            ? (await uow.PilgrimFeedbacks.FindAsync(f => applications.Select(a => a.Id).Contains(f.PilgrimId), ct)).ToDictionary(f => f.PilgrimId)
            : new();

        return Ok(ApiResponse<object>.Ok(new
        {
            User = new { user.Id, user.FullName, user.Email, user.PhoneNumber, user.Country },
            TotalTours     = applications.Count,
            CompletedTours = applications.Count(p => p.Status == ApplicationStatus.Approved),
            Applications   = applications.Select(p =>
            {
                tours.TryGetValue(p.TourId ?? Guid.Empty, out var tour);
                packages.TryGetValue(p.PackageId ?? Guid.Empty, out var pkg);
                feedbacks.TryGetValue(p.Id, out var feedback);
                return new
                {
                    p.Id, p.Status, p.CreatedAt, p.FamilyMemberCount,
                    p.ArrivalDate, p.DepartureDate,
                    Tour    = tour    is null ? null : new { tour.TourName, tour.TourType, tour.Year, tour.Status },
                    Package = pkg     is null ? null : new { pkg.PackageName, pkg.Price, pkg.Currency },
                    Feedback = feedback is null ? null : new { feedback.OverallRating, feedback.Comment, feedback.WouldReturn, feedback.SubmittedAt },
                };
            })
        }));
    }

    // ── Pilgrim: submit feedback ──────────────────────────────────────────────
    [HttpPost("{tourId:guid}/feedback")]
    [Authorize(Roles = "Pilgrim")]
    public async Task<IActionResult> SubmitFeedback(Guid tourId, [FromBody] FeedbackRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized();

        var pilgrim = (await uow.Pilgrims.FindAsync(p => p.UserId == userId && p.TourId == tourId, ct)).FirstOrDefault();
        if (pilgrim is null) return NotFound(ApiResponse<object>.Fail("No application found for this tour."));

        var existing = (await uow.PilgrimFeedbacks.FindAsync(f => f.PilgrimId == pilgrim.Id, ct)).FirstOrDefault();
        if (existing is not null) return BadRequest(ApiResponse<object>.Fail("Feedback already submitted."));

        var feedback = new PilgrimFeedback
        {
            PilgrimId           = pilgrim.Id,
            TourId              = tourId,
            OverallRating       = req.OverallRating,
            HotelRating         = req.HotelRating,
            TransportRating     = req.TransportRating,
            OrganizationRating  = req.OrganizationRating,
            FoodRating          = req.FoodRating,
            Comment             = req.Comment,
            Suggestions         = req.Suggestions,
            WouldRecommend      = req.WouldRecommend,
            WouldReturn         = req.WouldReturn,
            PreferredPackageNextYear = req.PreferredPackageNextYear,
            SubmittedAt         = DateTime.UtcNow,
        };
        await uow.PilgrimFeedbacks.AddAsync(feedback, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { }, "JazakAllah Khair! Your feedback has been submitted."));
    }

    // ── Admin: get all feedback for a tour ────────────────────────────────────
    [HttpGet("{tourId:guid}/feedback")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetFeedbacks(Guid tourId, CancellationToken ct)
    {
        var feedbacks = (await uow.PilgrimFeedbacks.FindAsync(f => f.TourId == tourId, ct)).ToList();
        if (!feedbacks.Any()) return Ok(ApiResponse<object>.Ok(new { Summary = new { }, Feedbacks = Array.Empty<object>() }));

        var pilgrimIds = feedbacks.Select(f => f.PilgrimId).ToList();
        var pilgrims   = (await uow.Pilgrims.FindAsync(p => pilgrimIds.Contains(p.Id), ct)).ToList();
        var userIds    = pilgrims.Select(p => p.UserId).ToList();
        var users      = (await uow.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);
        var pilgrimMap = pilgrims.ToDictionary(p => p.Id);

        return Ok(ApiResponse<object>.Ok(new
        {
            Summary = new
            {
                Total            = feedbacks.Count,
                AverageOverall   = Math.Round(feedbacks.Average(f => f.OverallRating), 1),
                AverageHotel     = feedbacks.Any(f => f.HotelRating.HasValue) ? Math.Round(feedbacks.Where(f => f.HotelRating.HasValue).Average(f => f.HotelRating!.Value), 1) : (double?)null,
                AverageTransport = feedbacks.Any(f => f.TransportRating.HasValue) ? Math.Round(feedbacks.Where(f => f.TransportRating.HasValue).Average(f => f.TransportRating!.Value), 1) : (double?)null,
                WouldReturn      = feedbacks.Count(f => f.WouldReturn),
                WouldRecommend   = feedbacks.Count(f => f.WouldRecommend),
            },
            Feedbacks = feedbacks.Select(f =>
            {
                pilgrimMap.TryGetValue(f.PilgrimId, out var p);
                users.TryGetValue(p?.UserId ?? Guid.Empty, out var u);
                return new
                {
                    f.OverallRating, f.HotelRating, f.TransportRating, f.OrganizationRating, f.FoodRating,
                    f.Comment, f.Suggestions, f.WouldReturn, f.WouldRecommend,
                    f.PreferredPackageNextYear, f.SubmittedAt,
                    PilgrimName = u?.FullName ?? "—",
                };
            })
        }));
    }

    // ── Public: get tour day schedule ────────────────────────────────────────
    [HttpGet("{id:guid}/schedule")]
    [Authorize]
    public async Task<IActionResult> GetSchedule(Guid id, CancellationToken ct)
    {
        var schedule = await uow.TourDaySchedules.FindAsync(s => s.TourId == id, ct);
        return Ok(ApiResponse<object>.Ok(schedule.OrderBy(s => s.DayNumber).Select(s => new
        {
            s.DayNumber, s.Date, s.City, s.Country, s.Hotel,
            s.Activity, s.ActivityUrdu, s.Notes, s.IsKeyDay, s.Icon
        })));
    }

    // ── Admin: save full day schedule (bulk upsert) ──────────────────────────
    [HttpPost("{tourId:guid}/day-schedules")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SaveDaySchedules(Guid tourId, [FromBody] List<SaveDayScheduleRequest> days, CancellationToken ct)
    {
        var tour = await uow.Tours.GetByIdAsync(tourId, ct);
        if (tour is null) return NotFound(ApiResponse<object>.Fail("Tour not found."));

        var existing = (await uow.TourDaySchedules.FindAsync(s => s.TourId == tourId, ct)).ToList();

        foreach (var req in days)
        {
            var record = existing.FirstOrDefault(e => e.DayNumber == req.DayNumber);
            if (record is null)
            {
                record = new TourDaySchedule { TourId = tourId };
                await uow.TourDaySchedules.AddAsync(record, ct);
            }
            record.DayNumber     = req.DayNumber;
            record.Date          = DateTime.SpecifyKind(req.Date.Date, DateTimeKind.Utc);
            record.City          = req.City;
            record.Country       = req.Country ?? "Iraq";
            record.Hotel         = req.Hotel;
            record.Activity      = req.Activity;
            record.ActivityUrdu  = req.ActivityUrdu;
            record.Notes         = req.Notes;
            record.IsKeyDay      = req.IsKeyDay;
            record.Icon          = req.Icon;
            if (record.Id != Guid.Empty) uow.TourDaySchedules.Update(record);
        }

        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { saved = days.Count }, "Day schedule saved."));
    }

    // ── Admin: send tour date reminder to all approved pilgrims ──────────────
    [HttpPost("{tourId:guid}/send-reminder")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SendTourReminder(Guid tourId, CancellationToken ct)
    {
        var tour = await uow.Tours.GetByIdAsync(tourId, ct);
        if (tour is null) return NotFound(ApiResponse<object>.Fail("Tour not found."));

        var pilgrims = (await uow.Pilgrims.FindAsync(p => p.TourId == tourId && p.Status == ApplicationStatus.Approved, ct)).ToList();
        var userIds  = pilgrims.Select(p => p.UserId).ToList();
        var users    = (await uow.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);

        int notifSent = 0;
        foreach (var pilgrim in pilgrims)
        {
            if (!users.TryGetValue(pilgrim.UserId, out var user)) continue;
            var arrivalFmt = pilgrim.ArrivalDate.ToString("dd MMM yyyy");
            await uow.Notifications.AddAsync(new Notification
            {
                UserId  = pilgrim.UserId,
                Title   = $"✈️ Your Tour Date is {arrivalFmt}",
                Message = $"Assalam-o-Alaikum {user.FullName}! Your Arbaeen 2026 journey ({tour.TourName}) starts on {arrivalFmt}. Please make sure your travel documents are ready. Jazak Allah Khair.",
                Type    = NotificationType.Push,
                Event   = NotificationEvent.General
            }, ct);

            var phone = user.WhatsAppNumber ?? user.PhoneNumber;
            if (!string.IsNullOrEmpty(phone))
                await whatsApp.SendMessageAsync(phone,
                    $"Assalam-o-Alaikum *{user.FullName}*! 🕌\n\n" +
                    $"*Misbah ul Hoda — Arbaeen 2026*\n\n" +
                    $"✈️ Aap ki tour *{tour.TourName}* ki date yaad dilana chahte hain:\n" +
                    $"📅 Arrival: *{arrivalFmt}*\n\n" +
                    $"Apne travel documents tayyar rakhein.\n" +
                    $"🌿 Jazak Allah Khair");
            notifSent++;
        }

        await uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { sent = notifSent }, $"Reminder sent to {notifSent} pilgrims."));
    }

    // ── Admin: live pilgrim tracking for a tour ───────────────────────────────
    [HttpGet("{id:guid}/live-tracking")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetLiveTracking(Guid id, CancellationToken ct)
    {
        var today    = DateTime.UtcNow.Date;
        var schedule = (await uow.TourDaySchedules.FindAsync(s => s.TourId == id, ct))
                           .OrderBy(s => s.DayNumber).ToList();
        var todaySchedule = schedule.FirstOrDefault(s => s.Date.Date == today);

        var pilgrims = (await uow.Pilgrims.FindAsync(
            p => p.TourId == id && p.Status == ApplicationStatus.Approved, ct)).ToList();

        var userIds = pilgrims.Select(p => p.UserId).ToList();
        var users   = (await uow.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);

        return Ok(ApiResponse<object>.Ok(new
        {
            TourId           = id,
            Today            = today.ToString("yyyy-MM-dd"),
            TodayCity        = todaySchedule?.City ?? "—",
            TodayActivity    = todaySchedule?.Activity,
            TodayDayNumber   = todaySchedule?.DayNumber,
            TodayIcon        = todaySchedule?.Icon,
            TotalPilgrims    = pilgrims.Count,
            ActivePilgrims   = pilgrims.Count(p => p.ArrivalDate.Date <= today && p.DepartureDate.Date >= today),
            Schedule         = schedule.Select(s => new { s.DayNumber, s.Date, s.City, s.Country, s.Hotel, s.Activity, s.IsKeyDay, s.Icon }),
            Pilgrims         = pilgrims.Select(p =>
            {
                users.TryGetValue(p.UserId, out var u);
                var isActive   = p.ArrivalDate.Date <= today && p.DepartureDate.Date >= today;
                var dayInTour  = schedule.FirstOrDefault(s => s.Date.Date == today);
                return new
                {
                    p.Id,
                    FullName            = u?.FullName ?? "—",
                    p.Country,
                    p.ArrivalDate,
                    p.DepartureDate,
                    Status              = p.Status.ToString(),
                    IsCurrentlyInIraq   = isActive,
                    TodayCity           = isActive ? dayInTour?.City
                                       : p.ArrivalDate > today ? "Not arrived yet" : "Departed",
                    WhatsApp            = u?.WhatsAppNumber,
                };
            })
        }));
    }

    // ── Private: send feedback emails ─────────────────────────────────────────
    private async Task SendFeedbackEmailsAsync(Guid tourId, string tourName, CancellationToken ct)
    {
        var pilgrims = (await uow.Pilgrims.FindAsync(p => p.TourId == tourId && !p.FeedbackSent, ct)).ToList();
        if (!pilgrims.Any()) return;

        var userIds = pilgrims.Select(p => p.UserId).ToList();
        var users   = (await uow.Users.FindAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);
        var frontendUrl = "http://localhost:5173";

        foreach (var pilgrim in pilgrims)
        {
            if (!users.TryGetValue(pilgrim.UserId, out var user)) continue;

            var feedbackLink = $"{frontendUrl}/pilgrim/feedback?tourId={tourId}";
            var html = BuildFeedbackEmail(user.FullName, tourName, feedbackLink);
            var waMsg = BuildFeedbackWhatsApp(user.FullName, tourName, feedbackLink);

            await emailService.SendAsync(user.Email, user.FullName,
                $"🌹 JazakAllah — Share Your Feedback for {tourName}", html);

            var phone = user.WhatsAppNumber ?? user.PhoneNumber;
            if (!string.IsNullOrEmpty(phone))
                await whatsApp.SendMessageAsync(phone, waMsg);

            pilgrim.FeedbackSent   = true;
            pilgrim.FeedbackSentAt = DateTime.UtcNow;
            uow.Pilgrims.Update(pilgrim);
        }

        await uow.SaveChangesAsync(ct);
    }

    private static string BuildFeedbackWhatsApp(string name, string tourName, string link) => string.Join("\n", new[]
    {
        $"Assalam-o-Alaikum *{name}*! 🕌",
        "",
        "*Misbah ul Hoda — Arbaeen 2026*",
        "",
        $"🌹 Alhamdulillah, *{tourName}* has been completed!",
        "JazakAllah Khair for being part of this blessed journey.",
        "",
        "We would love to hear about your experience.",
        "Please take 2 minutes to share your feedback:",
        $"👉 {link}",
        "",
        "Your feedback helps us serve you better in *{DateTime.UtcNow.Year + 1}* Insha'Allah.",
        "",
        "🌿 JazakAllah Khair",
    });

    private static string BuildNewTourWhatsApp(string name, Tour tour, List<TourPackage> packages)
    {
        var typeLabel = tour.TourType switch
        {
            TourType.Arbaeen => "Arbaeen", TourType.Umrah => "Umrah",
            TourType.Ashura  => "Ashura",  TourType.Ziyarat => "Ziyarat", _ => "Tour"
        };
        var pkgLines = packages.Any()
            ? packages.Select(p => $"  📦 *{p.PackageName}* — {p.Price:N0} {p.Currency} / person ({p.DurationDays} days)")
            : ["  (Packages being finalised — stay tuned!)"];

        return string.Join("\n",
        [
            $"Assalam-o-Alaikum *{name}*! 🕌",
            "",
            "*Misbah ul Hoda — New Tour Announced!*",
            "",
            $"🎉 We are pleased to announce *{tour.TourName}*",
            $"📅 {typeLabel} {tour.Year}",
            tour.StartDate != default ? $"🗓 {tour.StartDate:dd MMM yyyy} → {tour.EndDate:dd MMM yyyy}" : "",
            "",
            "*Available Packages:*",
            .. pkgLines,
            "",
            "Register now on your pilgrim portal:",
            "👉 http://localhost:5173/pilgrim/tours",
            "",
            "Limited seats — register early Insha'Allah! 🌿",
            "",
            "Misbah ul Hoda | misbahulhoda.org",
        ]);
    }

    private static string BuildNewTourEmail(string name, Tour tour, List<TourPackage> packages)
    {
        var typeLabel = tour.TourType switch
        {
            TourType.Arbaeen => "Arbaeen", TourType.Umrah => "Umrah",
            TourType.Ashura  => "Ashura",  TourType.Ziyarat => "Ziyarat", _ => "Tour"
        };
        var dateRange = tour.StartDate != default
            ? $"{tour.StartDate:dd MMM yyyy} &nbsp;→&nbsp; {tour.EndDate:dd MMM yyyy}" : "Dates TBD";

        var pkgRows = packages.Any()
            ? string.Join("", packages.Select(p => $"""
                <tr>
                  <td style="padding:10px 14px;border-bottom:1px solid rgba(180,83,9,0.15);color:#e2e8f0;font-weight:600">{p.PackageName}</td>
                  <td style="padding:10px 14px;border-bottom:1px solid rgba(180,83,9,0.15);color:#d97706;font-weight:700;white-space:nowrap">{p.Price:N0} {p.Currency}</td>
                  <td style="padding:10px 14px;border-bottom:1px solid rgba(180,83,9,0.15);color:#94a3b8">{p.DurationDays} days</td>
                  <td style="padding:10px 14px;border-bottom:1px solid rgba(180,83,9,0.15);color:#64748b">Max {p.MaxPilgrims}</td>
                </tr>
                """))
            : """<tr><td colspan="4" style="padding:14px;color:#64748b;text-align:center">Packages being finalised — check portal for updates</td></tr>""";

        return $"""
        <div style="font-family:sans-serif;max-width:560px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:1px solid rgba(180,83,9,0.3)">
          <div style="text-align:center;margin-bottom:24px">
            <h1 style="color:#d97706;margin:0;font-size:22px">🕌 Misbah ul Hoda</h1>
            <p style="color:#64748b;margin:4px 0 0;font-size:13px">misbahulhoda.org</p>
          </div>
          <h2 style="color:#f1f5f9;margin-top:0">New Tour Announced: {tour.TourName}</h2>
          <p>Assalam-o-Alaikum <strong>{name}</strong>,</p>
          <p>Alhamdulillah — we are excited to announce <strong>{tour.TourName}</strong> ({typeLabel} {tour.Year}).</p>
          <div style="background:rgba(180,83,9,0.1);border:1px solid rgba(180,83,9,0.3);border-radius:8px;padding:14px 18px;margin:20px 0">
            <p style="margin:0;color:#d97706;font-size:15px">📅 &nbsp;{dateRange}</p>
          </div>
          <h3 style="color:#d97706">Available Packages</h3>
          <table style="width:100%;border-collapse:collapse;background:rgba(255,255,255,0.03);border-radius:8px;overflow:hidden">
            <thead>
              <tr style="background:rgba(180,83,9,0.2)">
                <th style="padding:10px 14px;text-align:left;color:#d97706;font-size:13px">Package</th>
                <th style="padding:10px 14px;text-align:left;color:#d97706;font-size:13px">Price / Person</th>
                <th style="padding:10px 14px;text-align:left;color:#d97706;font-size:13px">Duration</th>
                <th style="padding:10px 14px;text-align:left;color:#d97706;font-size:13px">Seats</th>
              </tr>
            </thead>
            <tbody>{pkgRows}</tbody>
          </table>
          <div style="text-align:center;margin:28px 0">
            <a href="http://localhost:5173/pilgrim/tours"
               style="background:#b45309;color:#fff;padding:14px 36px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px;display:inline-block">
              Apply Now →
            </a>
          </div>
          <p style="color:#94a3b8;font-size:13px">Seats are limited. Register early to secure your preferred package — Insha'Allah.</p>
          <p style="color:#64748b;font-size:13px">As a previous pilgrim with Misbah ul Hoda, you receive early notifications. We look forward to serving you again.</p>
          <hr style="border-color:rgba(180,83,9,0.2);margin:20px 0"/>
          <p style="color:#475569;font-size:12px;text-align:center">Misbah ul Hoda — Pilgrimage Management · misbahulhoda.org</p>
        </div>
        """;
    }

    private static string BuildFeedbackEmail(string name, string tourName, string link) => $"""
        <div style="font-family:sans-serif;max-width:520px;margin:auto;background:#0f172a;color:#e2e8f0;padding:32px;border-radius:12px;border:1px solid rgba(180,83,9,0.3)">
          <h2 style="color:#d97706;margin-top:0">🌹 JazakAllah Khair — {tourName}</h2>
          <p>Assalam-o-Alaikum <strong>{name}</strong>,</p>
          <p>Alhamdulillah, <strong>{tourName}</strong> has been completed. We are grateful to Allah (SWT) for this blessed journey and honoured to have served you.</p>
          <p>We would love to hear about your experience so we can serve you and other pilgrims even better in the future.</p>
          <div style="text-align:center;margin:28px 0">
            <a href="{link}" style="background:#b45309;color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px">
              Share Your Feedback →
            </a>
          </div>
          <p style="color:#94a3b8;font-size:13px">This takes only 2 minutes. Your feedback directly shapes next year's tour — Insha'Allah.</p>
          <hr style="border-color:rgba(180,83,9,0.2);margin:20px 0"/>
          <p style="color:#475569;font-size:12px">Misbah ul Hoda — Arbaeen 2026 Pilgrimage Management</p>
        </div>
        """;
}

public record TourRequest(
    string TourName, int TourType, int Year,
    DateTime? StartDate, DateTime? EndDate,
    bool IsOpen, string? Description, int? Status);

public record PackageRequest(
    string PackageName, string? Description, decimal Price, string Currency,
    int MaxPilgrims, int DurationDays,
    int TripDuration, string? Destinations, string? RouteDetail,
    bool IncludesAirportTransfer, bool IncludesBreakfast, bool IncludesLunch, bool IncludesDinner,
    string? HotelInfo, string? Notes, bool IsActive = true);

public record TourApplyRequest(
    Guid? PackageId, string? PassportNumber, DateTime? PassportExpiry,
    string? VisaNumber, string? Country, int FamilyMemberCount,
    DateTime ArrivalDate, DateTime DepartureDate,
    string? ArrivalFlight, string? DepartureFlight,
    string? ArrivalAirport, string? DepartureAirport,
    string? ArrivalTime, string? DepartureTime,
    string? EmergencyContactName, string? EmergencyContactPhone);

public record FeedbackRequest(
    int OverallRating, int? HotelRating, int? TransportRating,
    int? OrganizationRating, int? FoodRating,
    string? Comment, string? Suggestions,
    bool WouldRecommend, bool WouldReturn,
    string? PreferredPackageNextYear);

public record SaveDayScheduleRequest(
    int DayNumber, DateTime Date, string City,
    string? Country, string? Hotel, string? Activity,
    string? ActivityUrdu, string? Notes, bool IsKeyDay, string? Icon);
