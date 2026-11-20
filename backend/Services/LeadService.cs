using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface ILeadService
{
    Task<LeadDto> CreateAsync(CreateLeadDto input);
    Task<List<LeadDto>> GetAllAsync(LeadStatus? status);
    Task<LeadDto?> UpdateStatusAsync(int id, LeadStatus status);
    Task<List<FollowUpDto>> GetFollowUpsAsync(int leadId);
    Task<FollowUpDto> RecordFollowUpAsync(int leadId);
    Task<FollowUpDto> GenerateAiReplyFollowUpAsync(int leadId, string buyerMessage);
    Task<LeadDto?> GetByIdAsync(int leadId);
}

public class LeadService : ILeadService
{
    private readonly AppDbContext _db;
    private readonly IWhatsAppService _whatsapp;
    private readonly IAiService _ai;

    public LeadService(AppDbContext db, IWhatsAppService whatsapp, IAiService ai)
    {
        _db = db;
        _whatsapp = whatsapp;
        _ai = ai;
    }

    public async Task<LeadDto?> GetByIdAsync(int leadId)
    {
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead is null) return null;
        var title = lead.PropertyListingId.HasValue
            ? await _db.Properties.AsNoTracking()
                .Where(p => p.Id == lead.PropertyListingId.Value)
                .Select(p => p.Title)
                .FirstOrDefaultAsync()
            : null;
        return ToDto(lead, title);
    }

    public async Task<FollowUpDto> GenerateAiReplyFollowUpAsync(int leadId, string buyerMessage)
    {
        var lead = await _db.Leads.FindAsync(leadId)
                   ?? throw new KeyNotFoundException("Lead not found.");

        var reply = await _ai.WhatsAppReplyAsync(
            new WhatsAppReplyInputDto(buyerMessage, lead.PropertyListingId));

        var sent = await _whatsapp.SendTextAsync(lead.Phone, reply.Reply);
        var link = sent ? null : _whatsapp.BuildLink(lead.Phone, reply.Reply);

        var followUp = new FollowUp
        {
            LeadId = lead.Id,
            Channel = "WhatsApp AI",
            Message = reply.Reply,
            ExternalUrl = link,
            Sent = sent
        };

        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();

        return new FollowUpDto(
            followUp.Id, followUp.LeadId, followUp.Channel,
            followUp.Message, followUp.ExternalUrl, followUp.Sent, followUp.CreatedAt);
    }

    public async Task<LeadDto> CreateAsync(CreateLeadDto input)
    {
        var lead = new Lead
        {
            Name = input.Name,
            Email = input.Email,
            Phone = input.Phone,
            Message = input.Message,
            PropertyListingId = input.PropertyListingId
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        await RecordFollowUpInternalAsync(lead);
        return ToDto(lead);
    }

    public async Task<List<LeadDto>> GetAllAsync(LeadStatus? status)
    {
        var q = _db.Leads.AsNoTracking().AsQueryable();
        if (status.HasValue)
            q = q.Where(l => l.Status == status.Value);

        var leads = await q
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        var propertyIds = leads
            .Where(l => l.PropertyListingId.HasValue)
            .Select(l => l.PropertyListingId!.Value)
            .Distinct()
            .ToList();

        var titles = await _db.Properties
            .AsNoTracking()
            .Where(p => propertyIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title);

        return leads.Select(l => ToDto(l, titles.GetValueOrDefault(l.PropertyListingId ?? 0))).ToList();
    }

    public async Task<LeadDto?> UpdateStatusAsync(int id, LeadStatus status)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead is null) return null;

        lead.Status = status;
        await _db.SaveChangesAsync();
        return ToDto(lead);
    }

    public async Task<List<FollowUpDto>> GetFollowUpsAsync(int leadId)
    {
        return await _db.FollowUps
            .AsNoTracking()
            .Where(f => f.LeadId == leadId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FollowUpDto(
                f.Id, f.LeadId, f.Channel, f.Message, f.ExternalUrl, f.Sent, f.CreatedAt))
            .ToListAsync();
    }

    public async Task<FollowUpDto> RecordFollowUpAsync(int leadId)
    {
        var lead = await _db.Leads.FindAsync(leadId)
                   ?? throw new KeyNotFoundException("Lead not found.");

        return await RecordFollowUpInternalAsync(lead);
    }

    private async Task<FollowUpDto> RecordFollowUpInternalAsync(Lead lead)
    {
        var property = lead.PropertyListingId.HasValue
            ? await _db.Properties.FindAsync(lead.PropertyListingId.Value)
            : null;

        var result = await _whatsapp.SendLeadFollowUpAsync(lead, property);

        var followUp = new FollowUp
        {
            LeadId = lead.Id,
            Channel = "WhatsApp",
            Message = result.Message,
            ExternalUrl = result.ExternalUrl,
            Sent = result.SentDirectly
        };

        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();

        return new FollowUpDto(
            followUp.Id,
            followUp.LeadId,
            followUp.Channel,
            followUp.Message,
            followUp.ExternalUrl,
            followUp.Sent,
            followUp.CreatedAt);
    }

    private static LeadDto ToDto(Lead l, string? propertyTitle = null)
    {
        var score = ScoreLead(l);
        return new LeadDto(
            l.Id,
            l.Name,
            l.Email,
            l.Phone,
            l.Message,
            l.PropertyListingId,
            propertyTitle,
            l.Status.ToString(),
            l.CreatedAt,
            score.Score,
            score.Label,
            score.Reasons);
    }

    private static (int Score, string Label, IReadOnlyList<string> Reasons) ScoreLead(Lead lead)
    {
        var score = 0;
        var reasons = new List<string>();
        var message = (lead.Message ?? string.Empty).ToLowerInvariant();

        var digits = new string((lead.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length >= 10)
        {
            score += 20;
            reasons.Add("Valid phone number");
        }

        if (lead.Email.Contains('@') && lead.Email.Contains('.'))
        {
            score += 10;
            reasons.Add("Email provided");
        }

        if (lead.PropertyListingId.HasValue)
        {
            score += 20;
            reasons.Add("Specific property inquiry");
        }

        if ((lead.Message ?? string.Empty).Length >= 30)
        {
            score += 10;
            reasons.Add("Detailed message");
        }

        var intentWords = new[] { "visit", "dekhna", "price", "rate", "book", "available", "interested", "call", "site" };
        if (intentWords.Any(w => message.Contains(w)))
        {
            score += 20;
            reasons.Add("Buying intent detected");
        }

        var age = DateTime.UtcNow - lead.CreatedAt.ToUniversalTime();
        if (age.TotalHours < 24)
        {
            score += 15;
            reasons.Add("New lead");
        }
        else if (age.TotalDays < 3)
        {
            score += 8;
            reasons.Add("Recent lead");
        }

        if (lead.Status is LeadStatus.Contacted or LeadStatus.Interested)
        {
            score += 5;
            reasons.Add("Already engaged");
        }

        score = Math.Min(score, 100);
        var label = score >= 70 ? "Hot" : score >= 40 ? "Warm" : "Cold";
        return (score, label, reasons);
    }
}
