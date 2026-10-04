using ConsuCare.Api.Data;
using ConsuCare.Api.Services;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsuCare.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokens;
    public AuthController(AppDbContext db, JwtTokenService tokens) { _db = db; _tokens = tokens; }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());
        if (user is null || !PasswordHasher.Verify(request.Password, user.Password))
            return Unauthorized("Invalid email or password.");
        return new AuthResponse(ToDto(user), _tokens.CreateToken(user));
    }

    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> Signup(SignupRequest request)
    {
        if (request.Role == UserRole.Supporter &&
            (string.IsNullOrWhiteSpace(request.ConditionType) ||
             request.DiagnosisYear < 1900 ||
             request.DiagnosisYear > DateTime.UtcNow.Year))
        {
            return BadRequest("Supporters must provide a condition and a valid diagnosis year.");
        }

        var email = request.Email.ToLower().Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("An account with that email already exists.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Password = PasswordHasher.Hash(request.Password),
            Role = request.Role,
            Community = request.Community.Trim(),
            IsPublic = request.Role != UserRole.Supporter || request.ShareHealthDetails
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (request.Role == UserRole.Supporter)
        {
            _db.SupporterProfiles.Add(new SupporterProfile
            {
                UserId = user.Id,
                ConditionType = request.ConditionType.Trim(),
                DiagnosisYear = request.DiagnosisYear,
                Verification = VerificationStatus.Pending,
                AppliedOn = DateTime.UtcNow,
                Available = true
            });
            await _db.SaveChangesAsync();
        }

        return new AuthResponse(ToDto(user), _tokens.CreateToken(user));
    }

    internal static UserDto ToDto(User u)
        => new(u.Id, u.FullName, u.Email, u.Role, u.Community, u.Bio,
            u.PhotoUrl, u.EmailNotifs, u.InAppNotifs, u.RequestAlerts, u.GoalAlerts, u.IsPublic);
}
