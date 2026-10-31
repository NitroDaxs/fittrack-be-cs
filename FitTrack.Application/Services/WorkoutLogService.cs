namespace FitTrack.Application.Services;

public class WorkoutLogService(
    IWorkoutLogRepository logs, IExerciseRepository exercises, IRoutineRepository routines, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<WorkoutLogDto>> GetPageAsync(int userId, string? search, string? range, PageRequest paging)
    {
        var page = await logs.GetPageAsync(userId, search, RangeStart(range), paging, defaultPerPage: 10);
        return page.Map(l => l.ToDto());
    }

    public async Task<WorkoutLogDto> GetAsync(int id, int userId)
    {
        var log = await logs.FindForUserAsync(id, userId)
                  ?? throw new NotFoundException("Workout not found.");

        return log.ToDto();
    }

    public async Task<WorkoutLogDto> LogAsync(int userId, LogWorkoutRequest request)
    {
        var exerciseIds = request.Exercises.Select(e => e.ExerciseId!.Value).Distinct().ToList();
        var knownIds = await exercises.GetExistingIdsAsync(exerciseIds);
        var unknownIds = exerciseIds.Except(knownIds).ToList();
        if (unknownIds.Count > 0)
        {
            throw new ValidationFailedException("exercises", $"Unknown exercise id(s): {string.Join(", ", unknownIds)}.");
        }

        if (request.WorkoutId is int workoutId && !await routines.WorkoutExistsAsync(workoutId))
        {
            throw new ValidationFailedException("workout_id", "The selected workout id is invalid.");
        }

        var log = new WorkoutLog
        {
            UserId = userId,
            WorkoutId = request.WorkoutId,
            Name = request.Name,
            Date = ToUtc(request.Date) ?? DateTime.UtcNow,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes,
        };

        foreach (var exercise in request.Exercises)
        {
            for (var i = 0; i < exercise.Sets.Count; i++)
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

        logs.Add(log);
        await unitOfWork.SaveChangesAsync();

        return (await logs.GetWithSetsAsync(log.Id)).ToDto();
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
