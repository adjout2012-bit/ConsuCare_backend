using ConsuCare.Api.Data;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsuCare.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    [HttpGet("verifications")]
    public async Task<List<VerificationDto>> Verifications()
        => await (
            from p in _db.SupporterProfiles
            join u in _db.Users on p.UserId equals u.Id
            orderby p.Verification == VerificationStatus.Pending descending, p.AppliedOn descending
            select new VerificationDto(p.Id, u.Id, u.FullName, u.Community, p.Title,
                p.ConditionType, p.DiagnosisYear, p.ManagementStrategies,
                p.AppliedOn, p.Verification)
        ).ToListAsync();

    [HttpPost("verifications/{profileId:int}/approve")]
    public Task<IActionResult> Approve(int profileId) => SetStatus(profileId, VerificationStatus.Approved);

    [HttpPost("verifications/{profileId:int}/reject")]
    public Task<IActionResult> Reject(int profileId) => SetStatus(profileId, VerificationStatus.Rejected);

    private async Task<IActionResult> SetStatus(int profileId, VerificationStatus status)
    {
        var profile = await _db.SupporterProfiles.FindAsync(profileId);
        if (profile is null) return NotFound();

        profile.Verification = status;

        _db.Notifications.Add(new Notification
        {
            UserId = profile.UserId,
            Kind = NotificationKind.System,
            Title = status == VerificationStatus.Approved
                ? "Your supporter application was approved"
                : "Your supporter application was not approved",
            Body = status == VerificationStatus.Approved
                ? "You now appear in the peer-support directory and can receive support requests."
                : "You can update your profile and reapply at any time.",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
