using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrustController : ControllerBase
{
    private readonly ITrustService _trust;

    public TrustController(ITrustService trust)
    {
        _trust = trust;
    }

    /// <summary>Compare a draft listing with live listings for duplicates and price anomalies.</summary>
    [Authorize]
    [HttpPost("duplicate-check")]
    public async Task<IActionResult> Check(DuplicateCheckInputDto input)
        => Ok(await _trust.CheckDraftAsync(input));

    /// <summary>Buyer-side market/document check for a single listing.</summary>
    [AllowAnonymous]
    [HttpGet("property/{id:int}")]
    public async Task<IActionResult> Report(int id)
        => Ok(await _trust.GetReportAsync(id));
}
