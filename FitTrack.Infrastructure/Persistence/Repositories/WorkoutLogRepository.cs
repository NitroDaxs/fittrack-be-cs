namespace FitTrack.Infrastructure.Persistence.Repositories;

public class WorkoutLogRepository(AppDbContext db) : IWorkoutLogRepository
{
    public Task<PagedResult<WorkoutLog>> GetPageAsync(int userId, string? search, DateTime? since, PageRequest paging, int defaultPerPage)
    {
        var logs = db.WorkoutLogs.AsNoTracking().WithSets().Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = search.ToSearchPattern();
            logs = logs.Where(l => EF.Functions.ILike(l.Name, pattern));
        }

        if (since is { } start)
        {
            logs = logs.Where(l => l.Date >= start);
        }

        return logs.OrderByDescending(l => l.Date).PaginateAsync(paging, defaultPerPage);
    }

    public Task<WorkoutLog?> FindForUserAsync(int id, int userId) =>
        db.WorkoutLogs.AsNoTracking().WithSets().FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

    public Task<WorkoutLog> GetWithSetsAsync(int id) =>
        db.WorkoutLogs.AsNoTracking().WithSets().FirstAsync(l => l.Id == id);

    public void Add(WorkoutLog log) => db.WorkoutLogs.Add(log);
}
