namespace FitTrack.Application.Abstractions.Persistence;

public interface IDashboardQueries
{
    Task<List<DateOnly>> GetTrainingDaysNewestFirstAsync(int userId);
    Task<int> CountWorkoutsAsync(int userId, DateTime from, DateTime before);
    Task<decimal?> GetLatestBodyWeightAsync(int userId);
    Task<List<DayCount>> GetWorkoutsPerDayAsync(int userId, DateTime since);

    /// <summary>The most recent measurements, oldest first.</summary>
    Task<List<TrendPoint>> GetMeasurementTrendAsync(int userId, int limit);

    Task<CreatorDto?> GetMostLoggedExerciseAsync(int userId);
    Task<List<ProgressionPoint>> GetProgressionAsync(int userId, int exerciseId);
}
