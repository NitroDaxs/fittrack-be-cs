using FitTrack.Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin")]
public class AdminStatsController(AdminStatsService stats) : ControllerBase
{
    [HttpGet("stats")]
    public Task<AdminStatsDto> Stats() => stats.GetStatsAsync();

    [HttpGet("activity")]
    public Task<List<ActivityDto>> Activity() => stats.GetActivityAsync();
}
