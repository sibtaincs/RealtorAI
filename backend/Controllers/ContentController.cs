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

    [HttpGet("{kind}/{id:int}")]
    public async Task<IActionResult> Get(string kind, int id)
    {
        var item = await _service.GetAsync(kind, id);
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
        var updated = await _service.UpdateAsync(input.Kind, id, input);
        return updated is null ? NotFound() : Ok(updated);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{kind}/{id:int}")]
    public async Task<IActionResult> Delete(string kind, int id) =>
        await _service.DeleteAsync(kind, id) ? NoContent() : NotFound();

    private static bool IsValid(ContentUpsertDto input, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(input.PayloadJson))
        {
            error = "Content JSON is required.";
            return false;
        }
        var (isValid, validationError) = ContentPayloadValidator.Validate(input.Kind, input.PayloadJson);
        if (!isValid)
        {
            error = string.IsNullOrWhiteSpace(validationError) ? "Invalid content payload." : validationError;
            return false;
        }
        return true;
    }
}
