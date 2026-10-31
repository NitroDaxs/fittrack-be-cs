using System.Linq.Expressions;

namespace FitTrack.Infrastructure.Persistence;

public static class QueryExtensions
{
    public static IQueryable<Exercise> WithDetails(this IQueryable<Exercise> query) =>
        query
            .Include(e => e.MuscleGroups).ThenInclude(l => l.MuscleGroup)
            .Include(e => e.Equipment).ThenInclude(l => l.Equipment)
            .Include(e => e.Media)
            .AsSplitQuery();

    public static IQueryable<Routine> WithProgramme(this IQueryable<Routine> query) =>
        query
            .Include(r => r.Creator)
            .Include(r => r.Workouts).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
            .ThenInclude(e => e.MuscleGroups).ThenInclude(l => l.MuscleGroup)
            .Include(r => r.Workouts).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
            .ThenInclude(e => e.Equipment).ThenInclude(l => l.Equipment)
            .AsSplitQuery();

    public static IQueryable<WorkoutLog> WithSets(this IQueryable<WorkoutLog> query) =>
        query.Include(l => l.SetLogs).ThenInclude(s => s.Exercise);

    public static IQueryable<T> SortBy<T>(
        this IQueryable<T> query, SortRequest sort, Dictionary<string, Expression<Func<T, object>>> columns, string fallback)
    {
        var key = sort.Column is not null && columns.ContainsKey(sort.Column) ? sort.Column : fallback;
        return sort.Direction == "asc" ? query.OrderBy(columns[key]) : query.OrderByDescending(columns[key]);
    }

    public static async Task<PagedResult<T>> PaginateAsync<T>(this IQueryable<T> query, PageRequest paging, int defaultPerPage)
    {
        var perPage = paging.Size(defaultPerPage);
        var page = paging.Number;

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync();

        return PagedResult.Create(items, page, perPage, total);
    }

    public static string ToSearchPattern(this string search) => $"%{search.Trim()}%";
}
