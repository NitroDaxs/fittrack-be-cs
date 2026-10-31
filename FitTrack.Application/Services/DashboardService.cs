namespace FitTrack.Application.Services;

public class DashboardService(IDashboardQueries queries, IUserRepository users, IExerciseRepository exercises)
{
    public async Task<DashboardDto> GetAsync(int userId)
    {
        var user = await users.GetAsync(userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var stats = new DashboardStats(
            CurrentStreak(await queries.GetTrainingDaysNewestFirstAsync(userId), today),
            await WorkoutsThisWeekAsync(userId, today),
            await queries.GetLatestBodyWeightAsync(userId));

        return new DashboardDto(
            new DashboardUser(user.Id, user.Name),
            stats,
            await queries.GetWorkoutsPerDayAsync(userId, DateTime.UtcNow.AddDays(-90)),
            await queries.GetMeasurementTrendAsync(userId, limit: 30),
            await DefaultProgressionAsync(userId));
    }

    public async Task<ProgressionDto> GetProgressionAsync(int userId, int exerciseId)
    {
        var exercise = await exercises.FindSummaryAsync(exerciseId)
                       ?? throw new NotFoundException("Exercise not found.");

        return new ProgressionDto(exercise, await queries.GetProgressionAsync(userId, exerciseId));
    }

    public static int CurrentStreak(IReadOnlyList<DateOnly> trainingDaysNewestFirst, DateOnly today)
    {
        if (trainingDaysNewestFirst.Count == 0 || trainingDaysNewestFirst[0] < today.AddDays(-1))
        {
            return 0;
        }

        var streak = 1;
        for (var i = 1; i < trainingDaysNewestFirst.Count; i++)
        {
            if (trainingDaysNewestFirst[i] != trainingDaysNewestFirst[i - 1].AddDays(-1))
            {
                break;
            }
            streak++;
        }

        return streak;
    }

    private Task<int> WorkoutsThisWeekAsync(int userId, DateOnly today)
    {
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var start = monday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return queries.CountWorkoutsAsync(userId, start, start.AddDays(7));
    }

    private async Task<ProgressionDto?> DefaultProgressionAsync(int userId)
    {
        var exercise = await queries.GetMostLoggedExerciseAsync(userId);

        return exercise is null
            ? null
            : new ProgressionDto(exercise, await queries.GetProgressionAsync(userId, exercise.Id));
    }
}
