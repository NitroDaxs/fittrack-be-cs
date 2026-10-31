using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me/workouts")]
public class WorkoutsController(WorkoutLogService workouts) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<WorkoutLogDto>> Index(
        [FromQuery] PageQuery paging, [FromQuery] string? search, [FromQuery] string? range) =>
        workouts.GetPageAsync(User.GetUserId(), search, range, paging.ToPageRequest());

    [HttpGet("{id:int}")]
    public Task<WorkoutLogDto> Show(int id) => workouts.GetAsync(id, User.GetUserId());

    [HttpPost]
    public async Task<IActionResult> Store(LogWorkoutRequest request)
    {
        var log = await workouts.LogAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(Show), new { id = log.Id }, log);
    }
}
