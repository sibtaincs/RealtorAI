using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private readonly IImageService _images;

    public UploadsController(IImageService images)
    {
        _images = images;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        try
        {
            return Ok(await _images.SaveAsync(file));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("multiple")]
    public async Task<IActionResult> UploadMany([FromForm] List<IFormFile> files)
    {
        var results = new List<object>();
        foreach (var file in files)
        {
            try
            {
                results.Add(await _images.SaveAsync(file));
            }
            catch (ArgumentException ex)
            {
                results.Add(new { error = ex.Message, file = file.FileName });
            }
        }
        return Ok(results);
    }
}
