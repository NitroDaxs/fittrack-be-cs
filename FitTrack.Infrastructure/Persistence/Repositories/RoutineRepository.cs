using System.Linq.Expressions;

namespace FitTrack.Infrastructure.Persistence.Repositories;

public class RoutineRepository(AppDbContext db) : IRoutineRepository
{
    private static readonly Dictionary<string, Expression<Func<Routine, object>>> Sortable = new()
    {
        ["title"] = r => r.Title,
        ["goal"] = r => r.Goal,
        ["level"] = r => r.Level,
        ["days_per_week"] = r => r.DaysPerWeek,
        ["daysPerWeek"] = r => r.DaysPerWeek,
        ["created_at"] = r => r.CreatedAt,
    };

    public Task<PagedResult<Routine>> GetPublicPageAsync(RoutineFilter filter, PageRequest paging, int defaultPerPage) =>
        Apply(db.Routines.AsNoTracking().Include(r => r.Creator).Where(r => r.IsPublic && r.IsPublished), filter)
            .OrderBy(r => r.Id)
            .PaginateAsync(paging, defaultPerPage);

    public Task<PagedResult<Routine>> GetPageAsync(RoutineFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage) =>
        Apply(db.Routines.AsNoTracking().Include(r => r.Creator), filter)
            .SortBy(sort, Sortable, fallback: "created_at")
            .PaginateAsync(paging, defaultPerPage);

    public Task<Routine?> FindPublicWithProgrammeAsync(string key)
    {
        var id = int.TryParse(key, out var parsed) ? parsed : 0;

        return db.Routines.AsNoTracking().WithProgramme()
            .FirstOrDefaultAsync(r => r.IsPublic && r.IsPublished && (r.Id == id || r.Slug == key));
    }

    public Task<Routine?> FindWithProgrammeAsync(int id) =>
        db.Routines.AsNoTracking().WithProgramme().FirstOrDefaultAsync(r => r.Id == id);

    public Task<Routine?> FindForUpdateAsync(int id) =>
        db.Routines.Include(r => r.Workouts).FirstOrDefaultAsync(r => r.Id == id);

    public Task<bool> ExistsAsync(int id) => db.Routines.AnyAsync(r => r.Id == id);

    public Task<bool> WorkoutExistsAsync(int workoutId) => db.Workouts.AnyAsync(w => w.Id == workoutId);

    public Task<bool> SlugTakenAsync(string slug, int? exceptId = null) =>
        db.Routines.AnyAsync(r => r.Slug == slug && (exceptId == null || r.Id != exceptId));

    public void Add(Routine routine) => db.Routines.Add(routine);

    public void ReplaceWorkouts(Routine routine, IEnumerable<Workout> workouts)
    {
        // RoutineId is nullable, so Clear() alone would leave the old days behind as orphans.
        db.Workouts.RemoveRange(routine.Workouts);
        routine.Workouts.Clear();
        routine.Workouts.AddRange(workouts);
    }

    public Task<int> DeleteAsync(int id) => db.Routines.Where(r => r.Id == id).ExecuteDeleteAsync();

    private static IQueryable<Routine> Apply(IQueryable<Routine> routines, RoutineFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = filter.Search.ToSearchPattern();
            routines = routines.Where(r =>
                EF.Functions.ILike(r.Title, pattern) || EF.Functions.ILike(r.Description ?? "", pattern));
        }

        if (filter.Goals.Count > 0)
        {
            routines = routines.Where(r => filter.Goals.Contains(r.Goal));
        }

        if (filter.Levels.Count > 0)
        {
            routines = routines.Where(r => filter.Levels.Contains(r.Level));
        }

        if (filter.MinDaysPerWeek is { } minDays)
        {
            routines = routines.Where(r => r.DaysPerWeek >= minDays);
        }

        if (filter.DaysPerWeek is { } days)
        {
            routines = routines.Where(r => r.DaysPerWeek == days);
        }

        return routines;
    }
}
