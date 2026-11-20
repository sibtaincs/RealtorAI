using RealEstateAI.Api.Dtos;

namespace RealEstateAI.Api.Services;

public interface IImageService
{
    Task<UploadResultDto> SaveAsync(IFormFile file);
}

public class ImageService : IImageService
{
    private static readonly string[] Allowed =
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    public ImageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<UploadResultDto> SaveAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("No file was uploaded.");

        if (file.Length > MaxBytes)
            throw new ArgumentException("Image must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(extension))
            throw new ArgumentException("Only JPG, PNG, WEBP and GIF images are allowed.");

        // WebRootPath is tied to the content root, not the process working directory,
        // so uploads always land where UseStaticFiles() can serve them from.
        var root = !string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? _env.WebRootPath
            : Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploads = Path.Combine(root, "uploads");
        Directory.CreateDirectory(uploads);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(uploads, fileName);

        await using var stream = File.Create(path);
        await file.CopyToAsync(stream);

        return new UploadResultDto($"/uploads/{fileName}", fileName);
    }
}
