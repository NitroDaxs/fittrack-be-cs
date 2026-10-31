namespace FitTrack.Application.Services.Admin;

public class AdminStatsService(IAdminStatsQueries queries, IStorageInfo storage)
{
    public async Task<AdminStatsDto> GetStatsAsync()
    {
        var counts = await queries.GetCountsAsync(newSince: DateTime.UtcNow.AddDays(-30));

        return new AdminStatsDto(
            counts.Exercises,
            Delta(counts.NewExercises),
            counts.Routines,
            Delta(counts.NewRoutines),
            counts.PublishedArticles,
            Delta(counts.NewArticles),
            counts.Users,
            Delta(counts.NewUsers),
            ApiUptime: 100, // placeholder: real uptime comes from a monitoring service
            StorageCapacity: storage.GetPercentUsed());
    }

    public async Task<List<ActivityDto>> GetActivityAsync()
    {
        var recent = await queries.GetRecentActivityAsync(perKind: 4);
        var items = new List<(DateTime At, ActivityDto Item)>();

        foreach (var e in recent.Exercises)
            items.Add((e.CreatedAt, new ActivityDto($"exercise-{e.Id}", "fitness_center", "primary", $"Exercise \"{e.Name}\" added to the library", TimeAgo(e.CreatedAt))));

        foreach (var a in recent.Articles)
            items.Add((a.CreatedAt, new ActivityDto($"article-{a.Id}", "article", "tertiary", $"Article \"{a.Title}\" {(a.Status == ArticleStatus.Published ? "published" : "drafted")}", TimeAgo(a.CreatedAt))));

        foreach (var u in recent.Users)
            items.Add((u.CreatedAt, new ActivityDto($"user-{u.Id}", "person_add", "secondary", $"{u.Name} joined as {u.Role.ToValue()}", TimeAgo(u.CreatedAt))));

        foreach (var l in recent.WorkoutLogs)
            items.Add((l.CreatedAt, new ActivityDto($"workout-{l.Id}", "exercise", "neutral", $"Workout \"{l.Name}\" logged", TimeAgo(l.CreatedAt))));

        return items.OrderByDescending(i => i.At).Take(8).Select(i => i.Item).ToList();
    }

    private static string Delta(int count) => count > 0 ? $"+{count} this month" : "No change";

    private static string TimeAgo(DateTime at)
    {
        var span = DateTime.UtcNow - at;
        return span.TotalMinutes < 1 ? "just now"
            : span.TotalHours < 1 ? $"{(int)span.TotalMinutes} minutes ago"
            : span.TotalDays < 1 ? $"{(int)span.TotalHours} hours ago"
            : $"{(int)span.TotalDays} days ago";
    }
}
