using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me/measurements")]
public class MeasurementsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<BodyMeasurementDto>> Index()
    {
        var userId = User.GetUserId();

        var measurements = await db.BodyMeasurements.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        return measurements.Select(m => m.ToDto()).ToList();
    }
    
    [HttpPost]
    public async Task<IActionResult> Store(LogMeasurementRequest request)
    {
        var measurement = new BodyMeasurement
        {
            UserId = User.GetUserId(),
            Date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Weight = request.Weight,
            BodyFatPct = request.BodyFatPct,
            CustomMeasurements = request.CustomMeasurements,
            PhotoUrl = request.PhotoUrl,
        };

        db.BodyMeasurements.Add(measurement);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, measurement.ToDto());
    }
}