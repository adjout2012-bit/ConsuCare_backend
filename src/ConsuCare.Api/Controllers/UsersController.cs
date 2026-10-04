using ConsuCare.Api.Data;
using ConsuCare.Api.Services;
using ConsuCare.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsuCare.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id)
    {
        var user = await _db.Users.FindAsync(id);
        return user is null ? NotFound() : AuthController.ToDto(user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.FullName = request.FullName.Trim();
        user.Email = request.Email.ToLower().Trim();
        user.Community = request.Community.Trim();
        user.Bio = request.Bio;
        user.PhotoUrl = request.PhotoUrl;
        await _db.SaveChangesAsync();

        return AuthController.ToDto(user);
    }

    [HttpPut("{id:int}/preferences")]
    public async Task<ActionResult<UserDto>> UpdatePreferences(int id, UpdatePreferencesRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.EmailNotifs = request.EmailNotifs;
        user.InAppNotifs = request.InAppNotifs;
        user.RequestAlerts = request.RequestAlerts;
        user.GoalAlerts = request.GoalAlerts;
        user.IsPublic = request.IsPublic;
        await _db.SaveChangesAsync();

        return AuthController.ToDto(user);
    }

    [HttpPost("{id:int}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (!PasswordHasher.Verify(request.CurrentPassword, user.Password))
            return BadRequest("Current password is incorrect.");
        if (request.NewPassword.Length < 8)
            return BadRequest("New password needs at least 8 characters.");

        user.Password = PasswordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
