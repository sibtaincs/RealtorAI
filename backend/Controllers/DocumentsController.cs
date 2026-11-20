using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents)
    {
        _documents = documents;
    }

    [AllowAnonymous]
    [HttpGet("property/{propertyId:int}")]
    public async Task<IActionResult> GetForProperty(int propertyId)
        => Ok(await _documents.GetAsync(propertyId));

    [Authorize]
    [HttpPost("property/{propertyId:int}")]
    public async Task<IActionResult> Add(int propertyId, CreateDocumentDto input)
    {
        var result = await _documents.AddAsync(propertyId, input);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateDocumentDto input)
    {
        var result = await _documents.UpdateAsync(id, input);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _documents.DeleteAsync(id) ? NoContent() : NotFound();
}
