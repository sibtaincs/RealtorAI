using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeasesController : ControllerBase
{
    private readonly ILeaseService _service;

    public LeasesController(ILeaseService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAsync(CurrentUserId()));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(LeaseInputDto input)
    {
        var result = await _service.CreateAsync(CurrentUserId(), input);
        return result is null ? BadRequest(new { message = "Invalid lease dates." }) : Ok(result);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LeaseInputDto input)
    {
        var result = await _service.UpdateAsync(id, CurrentUserId(), input);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id, CurrentUserId()) ? NoContent() : NotFound();

    [Authorize]
    [HttpPost("{id:int}/terminate")]
    public async Task<IActionResult> Terminate(int id)
    {
        var result = await _service.TerminateAsync(id, CurrentUserId());
        return result is null ? NotFound() : Ok(result);
    }

    private string CurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
