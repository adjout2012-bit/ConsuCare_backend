using ConsuCare.Api.Data;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsuCare.Api.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly AppDbContext _db;
    public GoalsController(AppDbContext db) => _db = db;

    [HttpGet("patient/{patientId:int}")]
    public async Task<List<GoalDto>> ForPatient(int patientId)
    {
        var goals = await _db.Goals.Include(g => g.Milestones)
            .Where(g => g.PatientId == patientId)
            .ToListAsync();
        var supporterNames = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        return goals.Select(g => ToDto(g, supporterNames)).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<GoalDto>> Create(CreateGoalRequest request)
    {
        var goal = new Goal
        {
            PatientId = request.PatientId,
            SupporterUserId = request.SupporterUserId,
            Title = request.Title.Trim(),
            Type = request.Type,
            Status = GoalStatus.InProgress,
            Milestones = request.Milestones
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => new Milestone { Title = m.Trim() }).ToList()
        };
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();

        var supporterNames = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        return ToDto(goal, supporterNames);
    }

    [HttpPost("milestones/{milestoneId:int}/toggle")]
    public async Task<ActionResult<GoalDto>> ToggleMilestone(int milestoneId)
    {
        var milestone = await _db.Milestones.FindAsync(milestoneId);
        if (milestone is null) return NotFound();

        milestone.IsDone = !milestone.IsDone;

        var goal = await _db.Goals.Include(g => g.Milestones)
            .FirstAsync(g => g.Id == milestone.GoalId);

        goal.Status = goal.Milestones.All(m => m.IsDone) ? GoalStatus.Completed : GoalStatus.InProgress;

        if (milestone.IsDone)
        {
            var done = goal.Milestones.Count(m => m.IsDone);
            var pct = (int)Math.Round(100.0 * done / goal.Milestones.Count);
            _db.Notifications.Add(new Notification
            {
                UserId = goal.PatientId,
                Kind = NotificationKind.MilestoneCompleted,
                Title = $"Milestone completed: {milestone.Title}",
                Body = goal.Status == GoalStatus.Completed
                    ? $"{goal.Title} is complete — congratulations!"
                    : $"{goal.Title} is now {pct}% complete.",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        var supporterNames = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        return ToDto(goal, supporterNames);
    }

    internal static GoalDto ToDto(Goal g, Dictionary<int, string> userNames)
        => new(g.Id, g.Title, g.Type, g.Status,
            g.SupporterUserId is int mid && userNames.TryGetValue(mid, out var name) ? name : "Unassigned",
            g.Milestones.OrderBy(m => m.Id).Select(m => new MilestoneDto(m.Id, m.Title, m.IsDone)).ToList());
}
