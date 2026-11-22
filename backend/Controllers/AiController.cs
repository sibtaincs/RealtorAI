using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiService _ai;

    public AiController(IAiService ai)
    {
        _ai = ai;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(AiChatDto input)
        => Ok(await _ai.ChatAsync(input.Message));

    [HttpPost("describe")]
    public async Task<IActionResult> Describe(DescribeDto input)
        => Ok(new { description = await _ai.GenerateDescriptionAsync(input) });

    [HttpPost("estimate")]
    public async Task<IActionResult> Estimate(PriceEstimateDto input)
        => Ok(await _ai.EstimatePriceAsync(input));

    [HttpPost("generate-listing")]
    public async Task<IActionResult> GenerateListing(GenerateListingInputDto input)
        => Ok(await _ai.GenerateListingAsync(input));

    [HttpPost("sell-price")]
    public async Task<IActionResult> SellPrice(SellPriceInputDto input)
        => Ok(await _ai.SellPriceAsync(input));

    [HttpPost("quality")]
    public async Task<IActionResult> Quality(ListingQualityInputDto input)
        => Ok(await _ai.QualityAsync(input));

    [HttpPost("whatsapp-reply")]
    public async Task<IActionResult> WhatsAppReply(WhatsAppReplyInputDto input)
        => Ok(await _ai.WhatsAppReplyAsync(input));

    [HttpPost("score-lead")]
    public async Task<IActionResult> ScoreLead(LeadScoreRequestDto input)
        => Ok(await _ai.ScoreLeadAsync(input));
}
