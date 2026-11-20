using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAsync(CurrentUserId()));

    [Authorize]
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
        => Ok(new { count = await _service.UnreadCountAsync(CurrentUserId()) });

    [Authorize]
    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
        => await _service.MarkReadAsync(id, CurrentUserId()) ? NoContent() : NotFound();

    [Authorize]
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
        => Ok(new { updated = await _service.MarkAllReadAsync(CurrentUserId()) });

    private string CurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
