namespace FitTrack.Application.Abstractions.Persistence;

public interface IWorkoutLogRepository
{
    Task<PagedResult<WorkoutLog>> GetPageAsync(int userId, string? search, DateTime? since, PageRequest paging, int defaultPerPage);
    Task<WorkoutLog?> FindForUserAsync(int id, int userId);

    /// <summary>Read-only with its sets; throws if it does not exist.</summary>
    Task<WorkoutLog> GetWithSetsAsync(int id);

    void Add(WorkoutLog log);
}
