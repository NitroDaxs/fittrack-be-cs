using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me/measurements")]
public class MeasurementsController(MeasurementService measurements) : ControllerBase
{
    [HttpGet]
    public Task<List<BodyMeasurementDto>> Index() => measurements.GetAllAsync(User.GetUserId());

    [HttpPost]
    public async Task<IActionResult> Store(LogMeasurementRequest request) =>
        StatusCode(StatusCodes.Status201Created, await measurements.LogAsync(User.GetUserId(), request));
}
