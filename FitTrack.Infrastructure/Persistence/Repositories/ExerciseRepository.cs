using System.Linq.Expressions;

namespace FitTrack.Infrastructure.Persistence.Repositories;

public class ExerciseRepository(AppDbContext db) : IExerciseRepository
{
    private static readonly Dictionary<string, Expression<Func<Exercise, object>>> Sortable = new()
    {
        ["name"] = e => e.Name,
        ["difficulty"] = e => e.Difficulty,
        ["exercise_type"] = e => e.ExerciseType,
        ["equipment"] = e => e.Equipment.Select(l => l.Equipment.Name).OrderBy(name => name).FirstOrDefault()!,
        ["created_at"] = e => e.CreatedAt,
    };

    public Task<PagedResult<Exercise>> GetPublishedPageAsync(ExerciseFilter filter, PageRequest paging, int defaultPerPage) =>
        Apply(db.Exercises.AsNoTracking().WithDetails().Where(e => e.IsPublished), filter)
            .OrderBy(e => e.Id)
            .PaginateAsync(paging, defaultPerPage);

    public Task<PagedResult<Exercise>> GetPageAsync(ExerciseFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage) =>
        Apply(db.Exercises.AsNoTracking().WithDetails(), filter)
            .SortBy(sort, Sortable, fallback: "created_at")
            .PaginateAsync(paging, defaultPerPage);

    public Task<Exercise?> FindPublishedAsync(string key)
    {
        var id = int.TryParse(key, out var parsed) ? parsed : 0;

        return db.Exercises.AsNoTracking().WithDetails()
            .FirstOrDefaultAsync(e => e.IsPublished && (e.Id == id || e.Slug == key));
    }

    public Task<Exercise> GetWithDetailsAsync(int id) =>
        db.Exercises.AsNoTracking().WithDetails().FirstAsync(e => e.Id == id);

    public Task<Exercise?> FindForUpdateAsync(int id) =>
        db.Exercises
            .Include(e => e.MuscleGroups)
            .Include(e => e.Equipment)
            .FirstOrDefaultAsync(e => e.Id == id);

    public Task<CreatorDto?> FindSummaryAsync(int id) =>
        db.Exercises.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new CreatorDto(e.Id, e.Name))
            .FirstOrDefaultAsync();

    public Task<bool> ExistsAsync(int id) => db.Exercises.AnyAsync(e => e.Id == id);

    public Task<List<int>> GetExistingIdsAsync(List<int> ids) =>
        db.Exercises.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync();

    public Task<bool> SlugTakenAsync(string slug, int? exceptId = null) =>
        db.Exercises.AnyAsync(e => e.Slug == slug && (exceptId == null || e.Id != exceptId));

    public Task<bool> AnyCreatedByAsync(int userId) => db.Exercises.AnyAsync(e => e.CreatedBy == userId);

    public void Add(Exercise exercise) => db.Exercises.Add(exercise);

    public Task<int> DeleteAsync(int id) => db.Exercises.Where(e => e.Id == id).ExecuteDeleteAsync();

    private static IQueryable<Exercise> Apply(IQueryable<Exercise> exercises, ExerciseFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = filter.Search.ToSearchPattern();
            exercises = exercises.Where(e =>
                EF.Functions.ILike(e.Name, pattern) || EF.Functions.ILike(e.Description ?? "", pattern));
        }

        if (filter.Difficulty is { } difficulty)
        {
            exercises = exercises.Where(e => e.Difficulty == difficulty);
        }

        if (filter.ExerciseType is { } type)
        {
            exercises = exercises.Where(e => e.ExerciseType == type);
        }

        if (filter.MuscleGroups is { Count: > 0 } keys)
        {
            var slugs = keys.Select(key => key.ToLowerInvariant()).ToList();
            var ids = keys.Select(key => int.TryParse(key, out var parsed) ? parsed : 0).ToList();
            exercises = exercises.Where(e => e.MuscleGroups.Any(l =>
                ids.Contains(l.MuscleGroupId) || slugs.Contains(l.MuscleGroup.Slug) || keys.Contains(l.MuscleGroup.Name)));
        }

        if (filter.Equipment is { Count: > 0 } equipmentKeys)
        {
            var slugs = equipmentKeys.Select(key => key.ToLowerInvariant()).ToList();
            var ids = equipmentKeys.Select(key => int.TryParse(key, out var parsed) ? parsed : 0).ToList();
            exercises = exercises.Where(e => e.Equipment.Any(l =>
                ids.Contains(l.EquipmentId) || slugs.Contains(l.Equipment.Slug) || equipmentKeys.Contains(l.Equipment.Name)));
        }

        return exercises;
    }
}
