using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitsController : ControllerBase
{
    private readonly IVisitService _service;

    public VisitsController(IVisitService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAsync(CurrentUserId()));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(VisitInputDto input)
    {
        var result = await _service.CreateAsync(CurrentUserId(), input);
        return result is null ? BadRequest(new { message = "Invalid visit status." }) : Ok(result);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VisitInputDto input)
    {
        var result = await _service.UpdateAsync(id, CurrentUserId(), input);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id, CurrentUserId()) ? NoContent() : NotFound();

    [Authorize]
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetVisitStatusDto status)
    {
        var result = await _service.SetStatusAsync(id, CurrentUserId(), status.Value);
        return result is null ? NotFound() : Ok(result);
    }

    private string CurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
