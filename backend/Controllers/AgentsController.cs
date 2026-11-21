using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _service;

    public AgentsController(IAgentService service)
    {
        _service = service;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId)
    {
        var profile = await _service.GetAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }
}
