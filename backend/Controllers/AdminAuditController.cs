using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Roles = "Admin")]
public class AdminAuditController : ControllerBase
{
    private readonly IAuditService _audit;
    public AdminAuditController(IAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 100)
        => Ok(await _audit.ListAsync(Math.Clamp(take, 1, 500)));
}