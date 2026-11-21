using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetApprovedAsync(int? propertyId, string? authorId);
    Task<ReviewDto> CreateAsync(string userId, CreateReviewDto input);
}

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetApprovedAsync(int? propertyId, string? authorId)
    {
        var query = _db.Reviews.AsNoTracking()
            .Where(r => r.ModerationStatus == ReviewModerationStatus.Approved);
        if (propertyId.HasValue)
            query = query.Where(r => r.PropertyListingId == propertyId.Value);
        if (!string.IsNullOrWhiteSpace(authorId))
            query = query.Where(r => r.AuthorId == authorId);

        return await query.OrderByDescending(r => r.CreatedAt).Take(50)
            .Select(r => new ReviewDto(
                r.Id, r.PropertyListingId, r.Author.FullName, r.Rating,
                r.Comment, r.ModerationStatus.ToString(), r.CreatedAt))
            .ToListAsync();
    }

    public async Task<ReviewDto> CreateAsync(string userId, CreateReviewDto input)
    {
        var review = new Review
        {
            AuthorId = userId,
            PropertyListingId = input.PropertyListingId,
            Rating = input.Rating,
            Comment = input.Comment.Trim(),
            ModerationStatus = ReviewModerationStatus.Pending
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        var author = await _db.Users.Where(u => u.Id == userId)
            .Select(u => u.FullName).FirstOrDefaultAsync() ?? "User";
        return new ReviewDto(review.Id, review.PropertyListingId, author, review.Rating,
            review.Comment, review.ModerationStatus.ToString(), review.CreatedAt);
    }
}
