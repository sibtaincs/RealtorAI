using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SavedSearchesController : ControllerBase
{
    private readonly ISavedSearchService _service;

    public SavedSearchesController(ISavedSearchService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAsync(CurrentUserId()));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateSavedSearchDto input)
    {
        var result = await _service.CreateAsync(CurrentUserId(), input);
        return Ok(result);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateSavedSearchDto input)
    {
        var result = await _service.UpdateAsync(id, CurrentUserId(), input);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id, CurrentUserId()) ? NoContent() : NotFound();

    [Authorize]
    [HttpPost("{id:int}/run")]
    public async Task<IActionResult> Run(int id)
        => Ok(new { newMatches = await _service.RunAsync(id) });

    private string CurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
