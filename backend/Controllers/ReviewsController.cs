using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _service;

    public ReviewsController(IReviewService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? propertyId, [FromQuery] string? authorId)
        => Ok(await _service.GetApprovedAsync(propertyId, authorId));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewDto input)
    {
        if (input.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(input.Comment) ||
            input.Comment.Length > 2000)
            return BadRequest("Rating 1 se 5 ke beech aur comment (max 2000 chars) zaroori hai.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? string.Empty;
        return Ok(await _service.CreateAsync(userId, input));
    }
}
