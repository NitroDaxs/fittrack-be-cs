using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<DashboardDto> Index() => dashboard.GetAsync(User.GetUserId());

    [HttpGet("exercises/{id:int}/progression")]
    public Task<ProgressionDto> Progression(int id) => dashboard.GetProgressionAsync(User.GetUserId(), id);
}
