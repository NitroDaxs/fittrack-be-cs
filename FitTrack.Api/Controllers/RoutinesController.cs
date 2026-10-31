using FitTrack.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api/routines")]
public class RoutinesController(RoutineService routines) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<RoutineDto>> Index([FromQuery] PageQuery paging, [FromQuery] string? search)
    {
        var filters = new RoutineSearch(
            search,
            Request.QueryList("goals", "goals[]", "goal", "goal[]"),
            Request.QueryList("levels", "levels[]", "level", "level[]"),
            Request.QueryList("daysPerWeek", "days_per_week", "days", "days[]").FirstOrDefault());

        return routines.GetPageAsync(filters, paging.ToPageRequest());
    }

    [HttpGet("{key}")]
    public Task<RoutineDto> Show(string key) => routines.GetAsync(key);
}
