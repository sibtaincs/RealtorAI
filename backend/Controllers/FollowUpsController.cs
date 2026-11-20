using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FollowUpsController : ControllerBase
{
    private readonly ILeadService _leads;

    public FollowUpsController(ILeadService leads)
    {
        _leads = leads;
    }

    [Authorize]
    [HttpGet("lead/{leadId:int}")]
    public async Task<IActionResult> GetForLead(int leadId)
        => Ok(await _leads.GetFollowUpsAsync(leadId));

    [Authorize]
    [HttpPost("lead/{leadId:int}")]
    public async Task<IActionResult> Create(int leadId)
    {
        try
        {
            return Ok(await _leads.RecordFollowUpAsync(leadId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Generates an AI WhatsApp reply to a buyer's message and records it as a follow-up.
    /// Requires an authenticated admin/agent.
    /// </summary>
    [Authorize]
    [HttpPost("lead/{leadId:int}/ai-reply")]
    public async Task<IActionResult> AiReply(int leadId, [FromBody] AiReplyRequest request)
    {
        try
        {
            var reply = await _leads.GenerateAiReplyFollowUpAsync(leadId, request.Message);
            return Ok(reply);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public record AiReplyRequest(string Message);
