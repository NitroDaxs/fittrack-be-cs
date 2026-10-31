namespace FitTrack.Infrastructure.Persistence.Repositories;

public class AdminStatsQueries(AppDbContext db) : IAdminStatsQueries
{
    public async Task<AdminCounts> GetCountsAsync(DateTime newSince) => new(
        await db.Exercises.CountAsync(),
        await db.Exercises.CountAsync(e => e.CreatedAt >= newSince),
        await db.Routines.CountAsync(),
        await db.Routines.CountAsync(r => r.CreatedAt >= newSince),
        await db.Articles.CountAsync(a => a.Status == ArticleStatus.Published),
        await db.Articles.CountAsync(a => a.CreatedAt >= newSince),
        await db.Users.CountAsync(),
        await db.Users.CountAsync(u => u.CreatedAt >= newSince));

    public async Task<RecentActivity> GetRecentActivityAsync(int perKind) => new(
        await db.Exercises.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(perKind).ToListAsync(),
        await db.Articles.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(perKind).ToListAsync(),
        await db.Users.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(perKind).ToListAsync(),
        await db.WorkoutLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(perKind).ToListAsync());
}
