using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/exercises")]
public class ExercisesController(ExerciseService exercises) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ExerciseDto>> Index([FromQuery] ExercisesQuery query) =>
        exercises.GetPageAsync(query.ToSearch(), query.ToPageRequest());

    [HttpGet("{key}")]
    public Task<ExerciseDto> Show(string key) => exercises.GetAsync(key);
}
