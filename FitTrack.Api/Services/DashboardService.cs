namespace FitTrack.Api.Services;

public class DashboardService(AppDbContext db)
{
    public async Task<DashboardDto> GetAsync(int userId)
    {
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var stats = new DashboardStats(
            CurrentStreak(await TrainingDaysNewestFirstAsync(userId), today),
            await WorkoutsThisWeekAsync(userId, today),
            await db.BodyMeasurements
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Date)
                .Select(m => m.Weight)
                .FirstOrDefaultAsync());

        return new DashboardDto(
            new DashboardUser(user.Id, user.Name),
            stats,
            await ConsistencyAsync(userId, days: 90),
            await MeasurementTrendAsync(userId, limit: 30),
            await DefaultProgressionAsync(userId));
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

    public async Task<List<ProgressionPoint>> ProgressionAsync(int userId, int exerciseId)
    {
        var rows = await db.SetLogs
            .Where(s => s.WorkoutLog.UserId == userId && s.ExerciseId == exerciseId)
            .GroupBy(s => s.WorkoutLog.Date.Date)
            .Select(g => new
            {
                Day = g.Key,
                MaxWeight = g.Max(s => s.Weight),
                Estimated1Rm = g.Max(s => s.Weight * (1 + s.Reps / 30m)), // Epley formula
            })
            .OrderBy(r => r.Day)
            .ToListAsync();

        return rows
            .Select(r => new ProgressionPoint(DateOnly.FromDateTime(r.Day), r.MaxWeight, Math.Round(r.Estimated1Rm, 2)))
            .ToList();
    }

    private async Task<List<DateOnly>> TrainingDaysNewestFirstAsync(int userId)
    {
        var days = await db.WorkoutLogs
            .Where(l => l.UserId == userId)
            .Select(l => l.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        return days.Select(DateOnly.FromDateTime).ToList();
    }

    private Task<int> WorkoutsThisWeekAsync(int userId, DateOnly today)
    {
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var start = monday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(7);

        return db.WorkoutLogs.CountAsync(l => l.UserId == userId && l.Date >= start && l.Date < end);
    }

    private async Task<List<DayCount>> ConsistencyAsync(int userId, int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var rows = await db.WorkoutLogs
            .Where(l => l.UserId == userId && l.Date >= since)
            .GroupBy(l => l.Date.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .OrderBy(r => r.Day)
            .ToListAsync();

        return rows.Select(r => new DayCount(DateOnly.FromDateTime(r.Day), r.Count)).ToList();
    }

    private async Task<List<TrendPoint>> MeasurementTrendAsync(int userId, int limit)
    {
        var latest = await db.BodyMeasurements
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Date)
            .Take(limit)
            .Select(m => new TrendPoint(m.Date, m.Weight, m.BodyFatPct))
            .ToListAsync();

        latest.Reverse();
        return latest;
    }

    private async Task<ProgressionDto?> DefaultProgressionAsync(int userId)
    {
        var exercise = await db.SetLogs
            .Where(s => s.WorkoutLog.UserId == userId)
            .GroupBy(s => new { s.ExerciseId, s.Exercise.Name })
            .OrderByDescending(g => g.Count())
            .Select(g => new CreatorDto(g.Key.ExerciseId, g.Key.Name))
            .FirstOrDefaultAsync();

        return exercise is null
            ? null
            : new ProgressionDto(exercise, await ProgressionAsync(userId, exercise.Id));
    }
}