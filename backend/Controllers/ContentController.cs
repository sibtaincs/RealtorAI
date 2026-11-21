using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/content")]
public class ContentController : ControllerBase
{
    private readonly IContentService _service;

    public ContentController(IContentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? kind) => Ok(await _service.ListAsync(kind));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _service.GetAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(ContentUpsertDto input)
    {
        if (!IsValid(input, out var error)) return BadRequest(error);
        return Ok(await _service.CreateAsync(input));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ContentUpsertDto input)
    {
        if (!IsValid(input, out var error)) return BadRequest(error);
        var updated = await _service.UpdateAsync(id, input);
        return updated is null ? NotFound() : Ok(updated);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await _service.DeleteAsync(id) ? NoContent() : NotFound();

    private static bool IsValid(ContentUpsertDto input, out string error)
    {
        error = string.Empty;
        if (!ContentKinds.IsValid(input.Kind))
        {
            error = "Unknown content kind.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(input.PayloadJson))
        {
            error = "Content JSON is required.";
            return false;
        }
        try
        {
            using var document = JsonDocument.Parse(input.PayloadJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object) return true;
        }
        catch (JsonException)
        {
            // handled below
        }
        error = "Content JSON must be a valid object.";
        return false;
    }
}
