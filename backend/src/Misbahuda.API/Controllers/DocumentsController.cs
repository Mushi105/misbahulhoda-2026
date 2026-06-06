using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.API.Controllers;

[Authorize]
public class DocumentsController(IMediator mediator, IUnitOfWork unitOfWork,
    ICurrentUserService currentUser, IWebHostEnvironment env) : BaseController(mediator)
{
    private static readonly string[] AllowedTypes = ["application/pdf", "image/jpeg", "image/png", "image/jpg"];

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var docs = await unitOfWork.TourDocuments.FindAsync(d => d.IsActive, cancellationToken);
        var userIds = docs.Select(d => d.UploadedById).Distinct().ToList();
        var users = userIds.Any()
            ? await unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken)
            : [];
        var userMap = users.ToDictionary(u => u.Id);

        var result = docs.OrderByDescending(d => d.CreatedAt).Select(d => new
        {
            d.Id, d.Title, d.Description, d.Category,
            d.FileName, d.ContentType, d.FileSizeBytes,
            d.CreatedAt,
            UploadedBy = userMap.TryGetValue(d.UploadedById, out var u) ? u.FullName : "Admin",
            SizeLabel = d.FileSizeBytes > 1_048_576
                ? $"{d.FileSizeBytes / 1_048_576.0:F1} MB"
                : $"{d.FileSizeBytes / 1024.0:F0} KB"
        });

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("upload")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequestSizeLimit(20_971_520)] // 20 MB
    public async Task<IActionResult> Upload(
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] string? category,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Please select a file."));

        if (!AllowedTypes.Contains(file.ContentType))
            return BadRequest(ApiResponse<object>.Fail("Only PDF, JPG, PNG files are allowed."));

        if (file.Length > 20_971_520)
            return BadRequest(ApiResponse<object>.Fail("File must be under 20 MB."));

        var uploadsDir = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "documents");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, storedName);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream, cancellationToken);

        var doc = new TourDocument
        {
            Title = title,
            Description = description,
            Category = category ?? "General",
            FileName = file.FileName,
            StoredFileName = storedName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedById = currentUser.UserId!.Value
        };

        await unitOfWork.TourDocuments.AddAsync(doc, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { doc.Id, doc.Title }, "Document uploaded successfully."));
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var doc = await unitOfWork.TourDocuments.GetByIdAsync(id, cancellationToken);
        if (doc is null || !doc.IsActive)
            return NotFound(ApiResponse<object>.Fail("Document not found."));

        var uploadsDir = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "documents");
        var filePath = Path.Combine(uploadsDir, doc.StoredFileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound(ApiResponse<object>.Fail("File not found on server."));

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath, cancellationToken);
        return File(bytes, doc.ContentType, doc.FileName);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var doc = await unitOfWork.TourDocuments.GetByIdAsync(id, cancellationToken);
        if (doc is null) return NotFound(ApiResponse<object>.Fail("Document not found."));

        var uploadsDir = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "documents");
        var filePath = Path.Combine(uploadsDir, doc.StoredFileName);
        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

        unitOfWork.TourDocuments.Delete(doc);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Document deleted."));
    }
}
