using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IDocumentService
{
    Task<List<DocumentItemDto>> GetAsync(int propertyId);
    Task<DocumentItemDto?> AddAsync(int propertyId, CreateDocumentDto input);
    Task<DocumentItemDto?> UpdateAsync(int id, UpdateDocumentDto input);
    Task<bool> DeleteAsync(int id);
}

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;

    public DocumentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DocumentItemDto>> GetAsync(int propertyId)
    {
        return await _db.DocumentChecklist
            .AsNoTracking()
            .Where(d => d.PropertyListingId == propertyId)
            .OrderBy(d => d.SortOrder)
            .Select(d => ToDto(d))
            .ToListAsync();
    }

    public async Task<DocumentItemDto?> AddAsync(int propertyId, CreateDocumentDto input)
    {
        if (!await _db.Properties.AnyAsync(p => p.Id == propertyId)) return null;

        var maxOrder = await _db.DocumentChecklist
            .Where(d => d.PropertyListingId == propertyId)
            .Select(d => (int?)d.SortOrder)
            .MaxAsync() ?? -1;

        var item = new DocumentChecklistItem
        {
            PropertyListingId = propertyId,
            Name = input.Name,
            Notes = input.Notes,
            SortOrder = maxOrder + 1
        };

        _db.DocumentChecklist.Add(item);
        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<DocumentItemDto?> UpdateAsync(int id, UpdateDocumentDto input)
    {
        var item = await _db.DocumentChecklist.FindAsync(id);
        if (item is null) return null;

        item.Notes = input.Notes;
        item.IsVerified = input.IsVerified;
        item.VerifiedAt = input.IsVerified ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.DocumentChecklist.FindAsync(id);
        if (item is null) return false;
        _db.DocumentChecklist.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    private static DocumentItemDto ToDto(DocumentChecklistItem d) => new(
        d.Id,
        d.PropertyListingId,
        d.Name,
        d.Notes,
        d.IsVerified,
        d.VerifiedAt,
        d.SortOrder);
}
