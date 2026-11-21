using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Models;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
        => Ok(await _service.GetSummaryAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty));

    [HttpGet("leads")]
    public async Task<IActionResult> Leads(
        [FromQuery] LeadStatus? status,
        [FromQuery] string? temperature,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _service.GetLeadsAsync(status, temperature, page, pageSize));

    [HttpGet("listings")]
    public async Task<IActionResult> Listings()
        => Ok(await _service.GetListingsAsync());
}
