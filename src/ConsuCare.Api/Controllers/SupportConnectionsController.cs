using ConsuCare.Api.Data;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsuCare.Api.Controllers;

[ApiController]
[Route("api/support-connections")]
public class SupportConnectionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SupportConnectionsController(AppDbContext db) => _db = db;

    /// Active and completed peer-support connections, plus pending requests, for a patient.
    [HttpGet("patient/{patientId:int}")]
    public async Task<List<SupportConnectionDto>> ForPatient(int patientId)
    {
        var connections = await _db.SupportConnections.Where(m => m.PatientId == patientId).ToListAsync();
        var pending = await _db.SupportRequests
            .Where(r => r.PatientId == patientId && r.Status == SupportRequestStatus.Pending)
            .ToListAsync();
        var goals = await _db.Goals.Include(g => g.Milestones)
            .Where(g => g.PatientId == patientId)
            .ToListAsync();
        var users = await _db.Users.ToDictionaryAsync(u => u.Id);
        var profiles = await _db.SupporterProfiles.ToDictionaryAsync(p => p.UserId);

        string RoleLine(int supporterUserId) =>
            users[supporterUserId].IsPublic && profiles.TryGetValue(supporterUserId, out var p)
                ? $"{users[supporterUserId].Community} · {p.ConditionType}"
                : users[supporterUserId].Community;

        var result = connections.Select(m =>
        {
            var goal = goals.FirstOrDefault(g => g.SupporterUserId == m.SupporterUserId);
            var progress = goal is null || goal.Milestones.Count == 0 ? 0
                : (int)Math.Round(100.0 * goal.Milestones.Count(x => x.IsDone) / goal.Milestones.Count);
            return new SupportConnectionDto(m.Id, m.SupporterUserId, users[m.SupporterUserId].FullName, RoleLine(m.SupporterUserId),
                goal?.Title ?? "No shared goal yet", progress, m.LastSessionAt, m.Status, false);
        }).ToList();

        result.AddRange(pending.Select(r =>
            new SupportConnectionDto(r.Id, r.SupporterUserId, users[r.SupporterUserId].FullName, RoleLine(r.SupporterUserId),
                $"Support requested for {RequestsController.Label(r.CareFocus)}", 0, null, ConnectionStatus.Active, true)));

        return result;
    }
}
