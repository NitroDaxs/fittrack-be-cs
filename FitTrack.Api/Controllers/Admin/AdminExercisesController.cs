using FitTrack.Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/exercises")]
public class AdminExercisesController(AdminExerciseService exercises) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ExerciseDto>> Index([FromQuery] AdminListQuery query) =>
        exercises.GetPageAsync(query.ToRequest(), query.ToPageRequest());

    [HttpPost]
    public async Task<IActionResult> Store(ExerciseRequest request) =>
        StatusCode(StatusCodes.Status201Created, await exercises.CreateAsync(User.GetUserId(), request));

    [HttpPut("{id:int}")]
    public Task<ExerciseDto> Update(int id, ExerciseRequest request) => exercises.UpdateAsync(id, request);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Destroy(int id)
    {
        await exercises.DeleteAsync(id);
        return Ok(new { status = "deleted" });
    }
}
