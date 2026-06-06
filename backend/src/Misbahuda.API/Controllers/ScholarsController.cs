using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

public class ScholarsController(IMediator mediator, IUnitOfWork unitOfWork) : BaseController(mediator)
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var scholars = await unitOfWork.Scholars
            .FindAsync(s => s.IsActive, cancellationToken);
        return Ok(ApiResponse<IEnumerable<Scholar>>.Ok(
            scholars.OrderBy(s => s.IsReciter).ThenBy(s => s.SortOrder).ThenBy(s => s.FullName)));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scholar = await unitOfWork.Scholars.GetByIdAsync(id, cancellationToken);
        if (scholar is null) return NotFound(ApiResponse<object>.Fail("Scholar not found."));
        return Ok(ApiResponse<Scholar>.Ok(scholar));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ScholarRequest request, CancellationToken cancellationToken)
    {
        var scholar = new Scholar
        {
            FullName = request.FullName,
            Title = request.Title ?? string.Empty,
            Specialty = request.Specialty,
            Language = request.Language,
            Location = request.Location,
            PhotoUrl = request.PhotoUrl,
            Bio = request.Bio,
            Quote = request.Quote,
            Tags = request.Tags,
            YoutubeSearch = request.YoutubeSearch,
            YoutubeUrl = request.YoutubeUrl,
            Website = request.Website,
            IsReciter = request.IsReciter,
            SortOrder = request.SortOrder,
            IsActive = true,
        };

        await unitOfWork.Scholars.AddAsync(scholar, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Scholar>.Ok(scholar, "Scholar added successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ScholarRequest request, CancellationToken cancellationToken)
    {
        var scholar = await unitOfWork.Scholars.GetByIdAsync(id, cancellationToken);
        if (scholar is null) return NotFound(ApiResponse<object>.Fail("Scholar not found."));

        scholar.FullName = request.FullName;
        scholar.Title = request.Title ?? string.Empty;
        scholar.Specialty = request.Specialty;
        scholar.Language = request.Language;
        scholar.Location = request.Location;
        scholar.PhotoUrl = request.PhotoUrl;
        scholar.Bio = request.Bio;
        scholar.Quote = request.Quote;
        scholar.Tags = request.Tags;
        scholar.YoutubeSearch = request.YoutubeSearch;
        scholar.YoutubeUrl = request.YoutubeUrl;
        scholar.Website = request.Website;
        scholar.IsReciter = request.IsReciter;
        scholar.SortOrder = request.SortOrder;
        scholar.IsActive = request.IsActive;

        unitOfWork.Scholars.Update(scholar);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Scholar>.Ok(scholar, "Scholar updated."));
    }

    [HttpPost("upload-photo")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded."));

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(ApiResponse<object>.Fail("Only JPG, PNG, and WebP images are allowed."));

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail("File must be under 5 MB."));

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "scholars");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream, cancellationToken);

        var url = $"/uploads/scholars/{fileName}";
        return Ok(ApiResponse<object>.Ok(new { url }, "Photo uploaded."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var scholar = await unitOfWork.Scholars.GetByIdAsync(id, cancellationToken);
        if (scholar is null) return NotFound(ApiResponse<object>.Fail("Scholar not found."));

        scholar.IsDeleted = true;
        unitOfWork.Scholars.Update(scholar);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Scholar removed."));
    }
}

public record ScholarRequest(
    string FullName,
    string? Title,
    string Specialty,
    string Language,
    string Location,
    string? PhotoUrl,
    string Bio,
    string? Quote,
    string? Tags,
    string? YoutubeSearch,
    string? YoutubeUrl,
    string? Website,
    bool IsReciter,
    int SortOrder,
    bool IsActive = true
);
