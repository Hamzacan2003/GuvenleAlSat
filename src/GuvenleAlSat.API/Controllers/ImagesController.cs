using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuvenleAlSat.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public ImagesController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost("upload-multiple")]
    [Authorize]
    public async Task<IActionResult> UploadMultiple([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { success = false, message = "Lütfen en az bir fotoğraf seçin." });

        if (files.Count > 10)
            return BadRequest(new { success = false, message = "Standart hesapla en fazla 10 fotoğraf yükleyebilirsiniz." });

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadDir = Path.Combine(webRoot, "uploads");
        if (!Directory.Exists(uploadDir))
            Directory.CreateDirectory(uploadDir);

        var uploadedUrls = new List<string>();

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowedExtensions.Contains(extension))
                    continue;

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                uploadedUrls.Add($"{baseUrl}/uploads/{fileName}");
            }
        }

        return Ok(new { success = true, data = uploadedUrls });
    }
}