using FitTrack.Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/users")]
public class AdminUsersController(AdminUserService users) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<UserDto>> Index([FromQuery] AdminListQuery query) =>
        users.GetPageAsync(query.ToRequest(), query.ToPageRequest());

    [HttpPost]
    public async Task<IActionResult> Store(AdminUserRequest request) =>
        StatusCode(StatusCodes.Status201Created, await users.CreateAsync(request));

    [HttpPut("{id:int}")]
    public Task<UserDto> Update(int id, AdminUserRequest request) => users.UpdateAsync(id, User.GetUserId(), request);

    [HttpPatch("{id:int}/role")]
    public Task<UserDto> UpdateRole(int id, AdminUserRequest request) =>
        users.UpdateRoleAsync(id, User.GetUserId(), request.Role);

    [HttpPatch("{id:int}/toggle-active")]
    public Task<UserDto> ToggleActive(int id) => users.ToggleActiveAsync(id, User.GetUserId());

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Destroy(int id)
    {
        await users.DeleteAsync(id, User.GetUserId());
        return Ok(new { status = "deleted" });
    }
}
