using FitTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class DashboardController(DashboardService dashboard, AppDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<DashboardDto> Index() => dashboard.GetAsync(User.GetUserId());

    [HttpGet("exercises/{id:int}/progression")]
    public async Task<ActionResult<ProgressionDto>> Progression(int id)
    {
        var exercise = await db.Exercises.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new CreatorDto(e.Id, e.Name))
            .FirstOrDefaultAsync();

        if (exercise is null)
        {
            return NotFound(new { message = "Exercise not found." });
        }

        return new ProgressionDto(exercise, await dashboard.ProgressionAsync(User.GetUserId(), id));
    }
}