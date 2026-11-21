using Microsoft.EntityFrameworkCore;
using RealEstateAI.Api.Data;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IReportService
{
    Task<ReportDto> CreateAsync(string userId, CreateReportDto input);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportDto> CreateAsync(string userId, CreateReportDto input)
    {
        var report = new Report
        {
            ReporterId = userId,
            PropertyListingId = input.PropertyListingId,
            Reason = input.Reason.Trim(),
            Status = ReportStatus.Open
        };
        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        var reporter = await _db.Users.Where(u => u.Id == userId)
            .Select(u => u.FullName).FirstOrDefaultAsync() ?? "User";
        return new ReportDto(report.Id, report.PropertyListingId, reporter,
            report.Reason, report.Status.ToString(), report.CreatedAt);
    }
}
