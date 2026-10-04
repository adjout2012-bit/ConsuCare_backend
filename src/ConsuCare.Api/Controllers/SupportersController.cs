using ConsuCare.Api.Data;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsuCare.Api.Controllers;

[ApiController]
[Route("api/supporters")]
[Authorize]
public class SupportersController : ControllerBase
{
    private readonly AppDbContext _db;
    public SupportersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<List<SupporterCardDto>> Discover([FromQuery] string? search = null)
    {
        var query =
            from p in _db.SupporterProfiles
            join u in _db.Users on p.UserId equals u.Id
            where p.Verification == VerificationStatus.Approved && u.IsPublic
            select new { p, u };

        var rows = await query.ToListAsync();

        var cards = rows.Select(x => new SupporterCardDto(
            x.u.Id, x.u.FullName, x.p.Title, x.p.ConditionType, x.p.DiagnosisYear, x.u.Community,
            x.p.Rating, x.p.ReviewCount, x.p.Available, x.p.ManagementStrategies, FirstSentence(x.u.Bio)));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            cards = cards.Where(c =>
                c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.Community.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.ConditionType.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.ManagementStrategies.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        return cards.OrderByDescending(c => c.Rating).ToList();
    }

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<SupporterProfileDto>> Profile(int userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsPublic);
        var profile = await _db.SupporterProfiles.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.Verification == VerificationStatus.Approved);
        if (user is null || profile is null) return NotFound();

        var reviews = await _db.Reviews
            .Where(r => r.SupporterUserId == userId)
            .Select(r => new ReviewDto(r.PatientName, r.Rating, r.Text))
            .ToListAsync();

        var card = new SupporterCardDto(user.Id, user.FullName, profile.Title, profile.ConditionType,
            profile.DiagnosisYear, user.Community, profile.Rating, profile.ReviewCount, profile.Available,
            profile.ManagementStrategies, FirstSentence(user.Bio));

        return new SupporterProfileDto(card, user.Bio, reviews);
    }

    [HttpPost("{userId:int}/reviews")]
    public async Task<ActionResult<SupporterProfileDto>> AddReview(int userId, CreateReviewRequest request)
    {
        var profile = await _db.SupporterProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        var isPublic = await _db.Users.AnyAsync(u => u.Id == userId && u.IsPublic);
        if (profile is null || !isPublic) return NotFound();

        var rating = Math.Clamp(request.Rating, 1, 5);
        _db.Reviews.Add(new Review
        {
            SupporterUserId = userId,
            PatientName = request.PatientName.Trim(),
            Rating = rating,
            Text = request.Text.Trim()
        });

        profile.Rating = (profile.Rating * profile.ReviewCount + rating) / (profile.ReviewCount + 1);
        profile.ReviewCount++;
        await _db.SaveChangesAsync();

        return await Profile(userId);
    }

    internal static string FirstSentence(string bio)
    {
        if (string.IsNullOrWhiteSpace(bio)) return "";
        var idx = bio.IndexOf('.');
        return idx > 0 && idx < bio.Length - 1 ? bio[..(idx + 1)] : bio;
    }
}
