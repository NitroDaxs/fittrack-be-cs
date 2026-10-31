namespace FitTrack.Application.Abstractions.Persistence;

public record ExerciseFilter(
    string? Search,
    Difficulty? Difficulty = null,
    ExerciseCategory? ExerciseType = null,
    IReadOnlyList<string>? MuscleGroups = null,
    IReadOnlyList<string>? Equipment = null);

public interface IExerciseRepository
{
    Task<PagedResult<Exercise>> GetPublishedPageAsync(ExerciseFilter filter, PageRequest paging, int defaultPerPage);
    Task<PagedResult<Exercise>> GetPageAsync(ExerciseFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage);

    /// <param name="key">An id or a slug.</param>
    Task<Exercise?> FindPublishedAsync(string key);

    /// <summary>Read-only with muscle groups, equipment and media; throws if it does not exist.</summary>
    Task<Exercise> GetWithDetailsAsync(int id);

    /// <summary>Change-tracked with muscle group and equipment links.</summary>
    Task<Exercise?> FindForUpdateAsync(int id);

    Task<CreatorDto?> FindSummaryAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<List<int>> GetExistingIdsAsync(List<int> ids);
    Task<bool> SlugTakenAsync(string slug, int? exceptId = null);
    Task<bool> AnyCreatedByAsync(int userId);
    void Add(Exercise exercise);

    /// <returns>The number of rows deleted.</returns>
    Task<int> DeleteAsync(int id);
}
