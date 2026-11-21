using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/moderation")]
[Authorize(Roles = "Admin")]
public class ModerationController : ControllerBase
{
    private readonly IModerationService _service;

    public ModerationController(IModerationService service)
    {
        _service = service;
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> PendingReviews() => Ok(await _service.GetPendingReviewsAsync());

    [HttpGet("reports")]
    public async Task<IActionResult> OpenReports() => Ok(await _service.GetOpenReportsAsync());

    [HttpPost("reviews/{id:int}/{decision}")]
    public async Task<IActionResult> ModerateReview(int id, string decision)
    {
        if (decision is not ("approve" or "reject")) return BadRequest();
        return await _service.ModerateReviewAsync(id, decision == "approve") ? Ok() : NotFound();
    }

    [HttpPost("reports/{id:int}/resolve")]
    public async Task<IActionResult> ResolveReport(int id)
        => await _service.ResolveReportAsync(id) ? Ok() : NotFound();
}
