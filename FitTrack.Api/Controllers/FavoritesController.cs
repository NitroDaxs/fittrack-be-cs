using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class FavoritesController(FavoritesService favorites) : ControllerBase
{
    [HttpPost("exercises/{id:int}/save")]
    public async Task<IActionResult> SaveExercise(int id)
    {
        await favorites.SaveExerciseAsync(id, User.GetUserId());
        return Ok(new { message = "Exercise saved successfully" });
    }

    [HttpDelete("exercises/{id:int}/save")]
    public async Task<IActionResult> UnsaveExercise(int id)
    {
        await favorites.UnsaveExerciseAsync(id, User.GetUserId());
        return Ok(new { message = "Exercise unsaved successfully" });
    }

    [HttpPost("routines/{id:int}/save")]
    public async Task<IActionResult> SaveRoutine(int id)
    {
        await favorites.SaveRoutineAsync(id, User.GetUserId());
        return Ok(new { message = "Routine saved successfully" });
    }

    [HttpDelete("routines/{id:int}/save")]
    public async Task<IActionResult> UnsaveRoutine(int id)
    {
        await favorites.UnsaveRoutineAsync(id, User.GetUserId());
        return Ok(new { message = "Routine unsaved successfully" });
    }
}
