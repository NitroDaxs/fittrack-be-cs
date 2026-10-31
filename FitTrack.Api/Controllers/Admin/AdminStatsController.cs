using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin")]
public class AdminStatsController(AppDbContext db, IHostEnvironment env) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<AdminStatsDto> Stats()
    {
        var since = DateTime.UtcNow.AddDays(-30);

        return new AdminStatsDto(
            await db.Exercises.CountAsync(),
            Delta(await db.Exercises.CountAsync(e => e.CreatedAt >= since)),
            await db.Routines.CountAsync(),
            Delta(await db.Routines.CountAsync(r => r.CreatedAt >= since)),
            await db.Articles.CountAsync(a => a.Status == ArticleStatus.Published),
            Delta(await db.Articles.CountAsync(a => a.CreatedAt >= since)),
            await db.Users.CountAsync(),
            Delta(await db.Users.CountAsync(u => u.CreatedAt >= since)),
            ApiUptime: 100, // placeholder: real uptime comes from a monitoring service
            StorageCapacity: DiskPercentUsed());
    }
    
    [HttpGet("activity")]
    public async Task<List<ActivityDto>> Activity()
    {
        var items = new List<(DateTime At, ActivityDto Item)>();

        foreach (var e in await db.Exercises.OrderByDescending(x => x.CreatedAt).Take(4).ToListAsync())
            items.Add((e.CreatedAt, new ActivityDto($"exercise-{e.Id}", "fitness_center", "primary", $"Exercise \"{e.Name}\" added to the library", TimeAgo(e.CreatedAt))));

        foreach (var a in await db.Articles.OrderByDescending(x => x.CreatedAt).Take(4).ToListAsync())
            items.Add((a.CreatedAt, new ActivityDto($"article-{a.Id}", "article", "tertiary", $"Article \"{a.Title}\" {(a.Status == ArticleStatus.Published ? "published" : "drafted")}", TimeAgo(a.CreatedAt))));

        foreach (var u in await db.Users.OrderByDescending(x => x.CreatedAt).Take(4).ToListAsync())
            items.Add((u.CreatedAt, new ActivityDto($"user-{u.Id}", "person_add", "secondary", $"{u.Name} joined as {u.Role.ToValue()}", TimeAgo(u.CreatedAt))));

        foreach (var l in await db.WorkoutLogs.OrderByDescending(x => x.CreatedAt).Take(4).ToListAsync())
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

    private int DiskPercentUsed()
    {
        var drive = new DriveInfo(Path.GetPathRoot(env.ContentRootPath)!);
        return drive.TotalSize == 0 ? 0 : (int)Math.Round(100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize);
    }
}