using FitTrack.Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/routines")]
public class AdminRoutinesController(AdminRoutineService routines) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<RoutineDto>> Index([FromQuery] AdminListQuery query) =>
        routines.GetPageAsync(query.ToRequest(), query.ToPageRequest());

    [HttpGet("{id:int}")]
    public Task<RoutineDto> Show(int id) => routines.GetAsync(id);

    [HttpPost]
    public async Task<IActionResult> Store(RoutineRequest request) =>
        StatusCode(StatusCodes.Status201Created, await routines.CreateAsync(User.GetUserId(), request));

    [HttpPut("{id:int}")]
    public Task<RoutineDto> Update(int id, RoutineRequest request) => routines.UpdateAsync(id, User.GetUserId(), request);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Destroy(int id)
    {
        await routines.DeleteAsync(id);
        return Ok(new { status = "deleted" });
    }
}
