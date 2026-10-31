using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me/workouts")]
public class WorkoutsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<WorkoutLogDto>> Index([FromQuery] PageQuery paging, [FromQuery] string? search,
        [FromQuery] string? range)
    {
        var userId = User.GetUserId();

        var logs = db.WorkoutLogs.AsNoTracking().WithSets().Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            logs = logs.Where(l => EF.Functions.ILike(l.Name, pattern));
        }

        if (RangeStart(range) is { } since)
        {
            logs = logs.Where(l => l.Date >= since);
        }

        return await logs
            .OrderByDescending(l => l.Date)
            .PaginateAsync(paging, defaultPerPage: 10, l => l.ToDto());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkoutLogDto>> Show(int id)
    {
        var userId = User.GetUserId();

        var log = await db.WorkoutLogs.AsNoTracking().WithSets()
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

        return log is null ? NotFound(new { message = "Workout not found." }) : log.ToDto();
    }

    [HttpPost]
    public async Task<IActionResult> Store(LogWorkoutRequest request)
    {
        var exerciseIds = request.Exercises.Select(e => e.ExerciseId!.Value).Distinct().ToList();
        var knownIds = await db.Exercises.Where(e => exerciseIds.Contains(e.Id)).Select(e => e.Id).ToListAsync();
        var unknownIds = exerciseIds.Except(knownIds).ToList();
        if (unknownIds.Count > 0)
        {
            return ApiErrors.Field("exercises", $"Unknown exercise id(s): {string.Join(", ", unknownIds)}.");
        }

        if (request.WorkoutId is int workoutId && !await db.Workouts.AnyAsync(w => w.Id == workoutId))
        {
            return ApiErrors.Field("workout_id", "The selected workout id is invalid.");
        }

        var log = new WorkoutLog
        {
            UserId = User.GetUserId(),
            WorkoutId = request.WorkoutId,
            Name = request.Name,
            Date = ToUtc(request.Date) ?? DateTime.UtcNow,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes,
        };

        foreach (var exercise in request.Exercises)
        {
            for (int i = 0; i < exercise.Sets.Count; i++)
            {
                log.SetLogs.Add(new SetLog
                {
                    ExerciseId = exercise.ExerciseId!.Value,
                    SetNumber = i + 1,
                    Weight = exercise.Sets[i].Weight ?? 0,
                    Reps = exercise.Sets[i].Reps ?? 0,
                });
            }
        }

        db.WorkoutLogs.Add(log);
        await db.SaveChangesAsync();

        var saved = await db.WorkoutLogs.AsNoTracking().WithSets().FirstAsync(l => l.Id == log.Id);
        return CreatedAtAction(nameof(Show), new { id = log.Id }, saved.ToDto());
    }
    
    private static DateTime? RangeStart(string? range)
    {
        var now = DateTime.UtcNow;
        return range switch
        {
            "Last 7 Days" => now.AddDays(-7),
            "Last 30 Days" => now.AddDays(-30),
            "This Month" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => null,
        };
    }

    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Unspecified } date => DateTime.SpecifyKind(date, DateTimeKind.Utc),
        { } date => date.ToUniversalTime(),
    };
}