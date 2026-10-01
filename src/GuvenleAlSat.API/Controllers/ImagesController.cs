using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuvenleAlSat.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly Cloudinary _cloudinary;

    public ImagesController(IConfiguration configuration)
    {
        // Render Dashboard > Environment Variables içine ekleyebilirsiniz:
        // CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET
        var cloudName = configuration["Cloudinary:CloudName"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") ?? "BURAYA_CLOUD_NAME";
        var apiKey = configuration["Cloudinary:ApiKey"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY") ?? "BURAYA_API_KEY";
        var apiSecret = configuration["Cloudinary:ApiSecret"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") ?? "BURAYA_API_SECRET";

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    [HttpPost("upload-multiple")]
    [Authorize]
    public async Task<IActionResult> UploadMultiple([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { success = false, message = "Lütfen en az bir fotoğraf seçin." });

        if (files.Count > 20)
            return BadRequest(new { success = false, message = "En fazla 20 fotoğraf yükleyebilirsiniz." });

        var uploadedUrls = new List<string>();

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowedExtensions.Contains(extension))
                    continue;

                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "guvenle-al-sat",
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult?.SecureUrl != null)
                {
                    uploadedUrls.Add(uploadResult.SecureUrl.ToString());
                }
            }
        }

        if (uploadedUrls.Count == 0)
            return BadRequest(new { success = false, message = "Fotoğraflar yüklenemedi." });

        return Ok(new { success = true, data = uploadedUrls });
    }
}