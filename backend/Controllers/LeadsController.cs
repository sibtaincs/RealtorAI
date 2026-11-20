using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _service;

    public LeadsController(ILeadService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create(CreateLeadDto input)
        => Ok(await _service.CreateAsync(input));

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LeadStatus? status)
        => Ok(await _service.GetAllAsync(status));

    [Authorize]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] LeadStatus status)
    {
        var result = await _service.UpdateStatusAsync(id, status);
        return result is null ? NotFound() : Ok(result);
    }
}
