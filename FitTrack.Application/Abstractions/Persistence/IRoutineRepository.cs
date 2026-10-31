namespace FitTrack.Application.Abstractions.Persistence;

public record RoutineFilter(
    string? Search,
    List<RoutineGoal> Goals,
    List<Difficulty> Levels,
    int? DaysPerWeek = null,
    int? MinDaysPerWeek = null);

public interface IRoutineRepository
{
    Task<PagedResult<Routine>> GetPublicPageAsync(RoutineFilter filter, PageRequest paging, int defaultPerPage);
    Task<PagedResult<Routine>> GetPageAsync(RoutineFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage);

    /// <param name="key">An id or a slug.</param>
    Task<Routine?> FindPublicWithProgrammeAsync(string key);

    /// <summary>Read-only with the full programme.</summary>
    Task<Routine?> FindWithProgrammeAsync(int id);

    /// <summary>Change-tracked with its workouts.</summary>
    Task<Routine?> FindForUpdateAsync(int id);

    Task<bool> ExistsAsync(int id);
    Task<bool> WorkoutExistsAsync(int workoutId);
    Task<bool> SlugTakenAsync(string slug, int? exceptId = null);
    void Add(Routine routine);
    void ReplaceWorkouts(Routine routine, IEnumerable<Workout> workouts);

    /// <returns>The number of rows deleted.</returns>
    Task<int> DeleteAsync(int id);
}
