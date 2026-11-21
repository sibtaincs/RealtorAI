using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/masterdata")]
public class MasterDataController : ControllerBase
{
    private readonly IMasterDataService _service;

    public MasterDataController(IMasterDataService service)
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
    public async Task<IActionResult> Create(MasterDataUpsertDto input)
    {
        if (!IsValid(input, out var error)) return BadRequest(error);
        if (MasterDataKinds.HasFixedValues(input.Kind)) return BadRequest("This list has fixed system values; edit the label instead.");
        if (await Duplicate(input)) return BadRequest("This value already exists in that list.");
        return Ok(await _service.CreateAsync(input));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MasterDataUpsertDto input)
    {
        if (!IsValid(input, out var error)) return BadRequest(error);
        if (await Duplicate(input, id)) return BadRequest("This value already exists in that list.");
        var existing = await _service.GetAsync(id);
        if (existing is null) return NotFound();
        if (MasterDataKinds.HasFixedValues(existing.Kind) &&
            !existing.Value.Equals(input.Value.Trim(), StringComparison.OrdinalIgnoreCase))
            return BadRequest("This list has fixed system values; edit the label instead.");
        var updated = await _service.UpdateAsync(id, input);
        return updated is null ? NotFound() : Ok(updated);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _service.GetAsync(id);
        if (existing is null) return NotFound();
        if (MasterDataKinds.HasFixedValues(existing.Kind))
            return BadRequest("This list has fixed system values; deactivate the row instead.");
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }

    private async Task<bool> Duplicate(MasterDataUpsertDto input, int? exceptId = null)
    {
        var items = await _service.ListAsync(input.Kind);
        return items.Any(m => m.Id != exceptId &&
            m.Value.Equals(input.Value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValid(MasterDataUpsertDto input, out string error)
    {
        error = string.Empty;
        if (!MasterDataKinds.IsValid(input.Kind))
        {
            error = "Unknown master data list.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(input.Value))
        {
            error = "Value is required.";
            return false;
        }
        if (input.Value.Trim().Length > 100)
        {
            error = "Value must be 100 characters or fewer.";
            return false;
        }
        return true;
    }
}
