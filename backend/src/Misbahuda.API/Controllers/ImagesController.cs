using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Misbahuda.Application.Common;

namespace Misbahuda.API.Controllers;

[Authorize(Roles = "SuperAdmin,Admin")]
public class ImagesController(IMediator mediator, IWebHostEnvironment env) : BaseController(mediator)
{
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"];
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]          // 10 MB hard cap at Kestrel level
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("File required."));

        if (!AllowedMimeTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(ApiResponse<object>.Fail("Only JPG, PNG, WEBP, GIF allowed."));

        if (file.Length > MaxBytes)
            return BadRequest(ApiResponse<object>.Fail("File too large (max 5 MB)."));

        var uploadsDir = Path.Combine(
            env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            "uploads", "images");
        Directory.CreateDirectory(uploadsDir);

        var ext       = Path.GetExtension(file.FileName).ToLower();
        var fileName  = $"{Guid.NewGuid()}{ext}";
        var filePath  = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        // Return the public URL
        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var url     = $"{baseUrl}/uploads/images/{fileName}";

        return Ok(ApiResponse<object>.Ok(new { url }, "Image uploaded."));
    }
}
