using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _service;
    private readonly UserManager<ApplicationUser> _users;

    public PropertiesController(IPropertyService service, UserManager<ApplicationUser> users)
    {
        _service = service;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PropertyQueryDto query)
        => Ok(await _service.GetAsync(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(PropertyInputDto input)
    {
        var gate = await EnsureApprovedAsync();
        if (gate is not null) return gate;
        return Ok(await _service.CreateAsync(input,
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PropertyInputDto input)
    {
        var gate = await EnsureApprovedAsync();
        if (gate is not null) return gate;
        var result = await _service.UpdateAsync(id, input);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var gate = await EnsureApprovedAsync();
        if (gate is not null) return gate;
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }

    private async Task<IActionResult?> EnsureApprovedAsync()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (user.ApprovalStatus != DealerApprovalStatus.Approved)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your dealer account is pending admin approval." });
        return null;
    }
}
