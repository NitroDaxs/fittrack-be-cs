namespace FitTrack.Infrastructure.Persistence.Repositories;

public class DashboardQueries(AppDbContext db) : IDashboardQueries
{
    public async Task<List<DateOnly>> GetTrainingDaysNewestFirstAsync(int userId)
    {
        var days = await db.WorkoutLogs
            .Where(l => l.UserId == userId)
            .Select(l => l.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        return days.Select(DateOnly.FromDateTime).ToList();
    }

    public Task<int> CountWorkoutsAsync(int userId, DateTime from, DateTime before) =>
        db.WorkoutLogs.CountAsync(l => l.UserId == userId && l.Date >= from && l.Date < before);

    public Task<decimal?> GetLatestBodyWeightAsync(int userId) =>
        db.BodyMeasurements
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Date)
            .Select(m => m.Weight)
            .FirstOrDefaultAsync();

    public async Task<List<DayCount>> GetWorkoutsPerDayAsync(int userId, DateTime since)
    {
        var rows = await db.WorkoutLogs
            .Where(l => l.UserId == userId && l.Date >= since)
            .GroupBy(l => l.Date.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .OrderBy(r => r.Day)
            .ToListAsync();

        return rows.Select(r => new DayCount(DateOnly.FromDateTime(r.Day), r.Count)).ToList();
    }

    public async Task<List<TrendPoint>> GetMeasurementTrendAsync(int userId, int limit)
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

    public Task<CreatorDto?> GetMostLoggedExerciseAsync(int userId) =>
        db.SetLogs
            .Where(s => s.WorkoutLog.UserId == userId)
            .GroupBy(s => new { s.ExerciseId, s.Exercise.Name })
            .OrderByDescending(g => g.Count())
            .Select(g => new CreatorDto(g.Key.ExerciseId, g.Key.Name))
            .FirstOrDefaultAsync();

    public async Task<List<ProgressionPoint>> GetProgressionAsync(int userId, int exerciseId)
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
}
