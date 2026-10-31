using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class ProfileController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<UserDto> Show() =>
        (await db.Users.AsNoTracking().FirstAsync(u => u.Id == User.GetUserId())).ToDto();

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var user = await db.Users.FirstAsync(u => u.Id == userId);

        if (request.Email is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email && u.Id != userId))
            {
                return ApiErrors.Field("email", "The email address is already used");
            }

            user.Email = email;
        }

        if (request.NewPassword is not null)
        {
            if (request.CurrentPassword is null ||
                !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return ApiErrors.Message("Current password is incorrect");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        user.Name = request.Name ?? user.Name;
        user.Units = request.Units ?? user.Units;

        await db.SaveChangesAsync();
        return Ok(user.ToDto());
    }

    [HttpGet("favorites")]
    [HttpGet("saved")]
    public async Task<FavoritesDto> Favorites()
    {
        var userId = User.GetUserId();

        var routines = await db.Routines.AsNoTracking().Include(r => r.Creator)
            .Where(r => db.SavedRoutines.Any(s => s.RoutineId == r.Id && s.UserId == userId))
            .ToListAsync();

        var exercises = await db.Exercises.AsNoTracking().WithDetails()
            .Where(e => db.SavedExercises.Any(s => s.ExerciseId == e.Id && s.UserId == userId))
            .ToListAsync();

        return new FavoritesDto(
            routines.Select(r => r.ToDto()).ToList(),
            exercises.Select(e => e.ToDto()).ToList());
    }
}