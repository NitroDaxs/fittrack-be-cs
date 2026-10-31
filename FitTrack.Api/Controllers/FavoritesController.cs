using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class FavoritesController(AppDbContext db) : ControllerBase
{
    [HttpPost("exercises/{id:int}/save")]
    public async Task<IActionResult> SaveExercise(int id)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == id))
        {
            return NotFound(new { message = "Exercise not found." });
        }

        var userId = User.GetUserId();
        if (!await db.SavedExercises.AnyAsync(s => s.ExerciseId == id && s.UserId == userId))
        {
            db.SavedExercises.Add(new SavedExercise { ExerciseId = id, UserId = userId });
            await db.SaveChangesAsync();
        }

        return Ok(new { message = "Exercise saved successfully" });
    }
    
    [HttpDelete("exercises/{id:int}/save")]
    public async Task<IActionResult> UnsaveExercise(int id)
    {
        var userId = User.GetUserId();
        await db.SavedExercises.Where(s => s.ExerciseId == id && s.UserId == userId).ExecuteDeleteAsync();
        return Ok(new { message = "Exercise unsaved successfully" });
    }
    
    [HttpPost("routines/{id:int}/save")]
    public async Task<IActionResult> SaveRoutine(int id)
    {
        if (!await db.Routines.AnyAsync(r => r.Id == id))
        {
            return NotFound(new { message = "Routine not found." });
        }

        var userId = User.GetUserId();
        if (!await db.SavedRoutines.AnyAsync(s => s.RoutineId == id && s.UserId == userId))
        {
            db.SavedRoutines.Add(new SavedRoutine { RoutineId = id, UserId = userId });
            await db.SaveChangesAsync();
        }

        return Ok(new { message = "Routine saved successfully" });
    }
    
    [HttpDelete("routines/{id:int}/save")]
    public async Task<IActionResult> UnsaveRoutine(int id)
    {
        var userId = User.GetUserId();
        await db.SavedRoutines.Where(s => s.RoutineId == id && s.UserId == userId).ExecuteDeleteAsync();
        return Ok(new { message = "Routine unsaved successfully" });
    }
}