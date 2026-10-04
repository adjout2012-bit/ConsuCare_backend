using ConsuCare.Api.Data;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsuCare.Api.Controllers;

[ApiController]
[Route("api/support-requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    public RequestsController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<SupportRequestDto>> Create(CreateSupportRequest request)
    {
        var patient = await _db.Users.FirstOrDefaultAsync(u =>
            u.Id == request.PatientId && u.Role == UserRole.Patient);
        var supporter = await _db.Users.FirstOrDefaultAsync(u =>
            u.Id == request.SupporterUserId && u.Role == UserRole.Supporter && u.IsPublic);
        var approvedProfile = await _db.SupporterProfiles.AnyAsync(p =>
            p.UserId == request.SupporterUserId && p.Verification == VerificationStatus.Approved);
        if (patient is null || supporter is null || !approvedProfile) return NotFound();

        var entity = new SupportRequest
        {
            PatientId = request.PatientId,
            SupporterUserId = request.SupporterUserId,
            CareFocus = request.CareFocus,
            Message = request.Message.Trim(),
            Frequency = request.Frequency,
            Status = SupportRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.SupportRequests.Add(entity);

        _db.Notifications.Add(new Notification
        {
            UserId = supporter.Id,
            Kind = NotificationKind.RequestReceived,
            Title = "New peer-support request received",
            Body = $"{patient.FullName} requested support with {Label(request.CareFocus)}.",
            CreatedAt = entity.CreatedAt
        });

        await _db.SaveChangesAsync();
        return ToDto(entity, patient, supporter);
    }

    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var request = await _db.SupportRequests.FindAsync(id);
        if (request is null) return NotFound();
        if (request.Status != SupportRequestStatus.Pending) return Conflict("Request already handled.");

        request.Status = SupportRequestStatus.Accepted;

        _db.SupportConnections.Add(new SupportConnection
        {
            PatientId = request.PatientId,
            SupporterUserId = request.SupporterUserId,
            Status = ConnectionStatus.Active,
            StartedAt = DateTime.UtcNow
        });

        var supporter = await _db.Users.FindAsync(request.SupporterUserId);
        _db.Notifications.Add(new Notification
        {
            UserId = request.PatientId,
            Kind = NotificationKind.RequestAccepted,
            Title = $"{supporter?.FullName} accepted your support request",
            Body = $"Your peer-support connection for {Label(request.CareFocus)} is now active — say hello.",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/decline")]
    public async Task<IActionResult> Decline(int id)
    {
        var request = await _db.SupportRequests.FindAsync(id);
        if (request is null) return NotFound();
        request.Status = SupportRequestStatus.Declined;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    internal static string Label(CareFocus type) => type switch
    {
        CareFocus.GeneralHealth => "General Health",
        CareFocus.PhysicalActivity => "Physical Activity",
        CareFocus.MentalWellbeing => "Mental Wellbeing",
        CareFocus.MedicationAdherence => "Medication Adherence",
        _ => type.ToString()
    };

    internal static SupportRequestDto ToDto(SupportRequest r, User patient, User supporter)
        => new(r.Id, r.PatientId, patient.FullName, patient.Community, r.SupporterUserId, supporter.FullName,
            r.CareFocus, r.Message, r.Frequency, r.Status, r.CreatedAt);
}
