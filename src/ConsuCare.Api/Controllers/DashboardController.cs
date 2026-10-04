using ConsuCare.Api.Data;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsuCare.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("patient/{patientId:int}")]
    public async Task<PatientDashboardDto> Patient(int patientId)
    {
        var connections = await _db.SupportConnections
            .Where(m => m.PatientId == patientId && m.Status == ConnectionStatus.Active)
            .ToListAsync();
        var supporterIds = connections.Select(m => m.SupporterUserId).ToList();

        var supporters = await (
            from p in _db.SupporterProfiles
            join u in _db.Users on p.UserId equals u.Id
            where supporterIds.Contains(p.UserId) && u.IsPublic
            select new SupporterCardDto(u.Id, u.FullName, p.Title, p.ConditionType, p.DiagnosisYear,
                u.Community, p.Rating, p.ReviewCount, p.Available, p.ManagementStrategies, "")
        ).ToListAsync();

        var goals = await _db.Goals.Include(g => g.Milestones)
            .Where(g => g.PatientId == patientId && g.Status == GoalStatus.InProgress)
            .ToListAsync();
        var names = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

        var unread = await _db.Messages.CountAsync(m => m.RecipientId == patientId && !m.IsRead);

        return new PatientDashboardDto(
            connections.Count,
            goals.Count,
            unread,
            supporters,
            goals.Select(g => GoalsController.ToDto(g, names)).ToList());
    }

    [HttpGet("supporter/{supporterUserId:int}")]
    public async Task<SupporterDashboardDto> Supporter(int supporterUserId)
    {
        var connections = await _db.SupportConnections
            .Where(m => m.SupporterUserId == supporterUserId && m.Status == ConnectionStatus.Active)
            .ToListAsync();

        var pending = await _db.SupportRequests
            .Where(r => r.SupporterUserId == supporterUserId && r.Status == SupportRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        var users = await _db.Users.ToDictionaryAsync(u => u.Id);
        var supporter = users[supporterUserId];

        var requests = pending
            .Select(r => RequestsController.ToDto(r, users[r.PatientId], supporter))
            .ToList();

        var patientIds = connections.Select(m => m.PatientId).ToList();
        var goals = await _db.Goals.Include(g => g.Milestones)
            .Where(g => g.SupporterUserId == supporterUserId && patientIds.Contains(g.PatientId))
            .ToListAsync();

        var patients = connections.Select(m =>
        {
            var patient = users[m.PatientId];
            var goal = goals.FirstOrDefault(g => g.PatientId == m.PatientId);
            var progress = goal is null || goal.Milestones.Count == 0 ? 0
                : (int)Math.Round(100.0 * goal.Milestones.Count(x => x.IsDone) / goal.Milestones.Count);
            return new PatientRowDto(patient.Id, patient.FullName, patient.Community,
                goal?.Title ?? "No goal yet", progress, m.LastSessionAt);
        }).ToList();

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var sessions = connections.Count(m => m.LastSessionAt >= monthStart);

        return new SupporterDashboardDto(connections.Count, pending.Count, sessions, requests, patients);
    }
}
