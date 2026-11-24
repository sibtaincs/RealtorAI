using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/admin/dealers")]
[Authorize(Roles = "Admin")]
public class AdminDealersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAuditService _audit;

    public AdminDealersController(UserManager<ApplicationUser> users, IAuditService audit)
    {
        _users = users;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status)
    {
        var filter = string.IsNullOrWhiteSpace(status) ? "Pending" : status;
        if (!Enum.TryParse<DealerApprovalStatus>(filter, true, out var parsed))
            return BadRequest("Unknown status.");

        var users = _users.Users
            .Where(u => u.ApprovalStatus == parsed)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new DealerApprovalDto(u.Id, u.FullName, u.Email!, u.AgencyName, u.ApprovalStatus.ToString(), u.CreatedAt, u.IsKycVerified))
            .ToList();

        return Ok(users);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.ApprovalStatus = DealerApprovalStatus.Approved;
        await _users.UpdateAsync(user);
        await _audit.LogAsync("DealerApproved", user.Id, user.Email, "Dealer", user.Id, $"Dealer approved: {user.Email}");
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.ApprovalStatus = DealerApprovalStatus.Rejected;
        await _users.UpdateAsync(user);
        await _audit.LogAsync("DealerRejected", user.Id, user.Email, "Dealer", user.Id, $"Dealer rejected: {user.Email}");
        return NoContent();
    }

    [HttpPost("{id}/kyc")]
    public async Task<IActionResult> VerifyKyc(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.IsKycVerified = true;
        user.ApprovalStatus = DealerApprovalStatus.Approved;
        await _users.UpdateAsync(user);
        await _audit.LogAsync("DealerKycVerified", user.Id, user.Email, "Dealer", user.Id, $"KYC verified: {user.Email}");
        return NoContent();
    }
}