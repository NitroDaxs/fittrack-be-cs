namespace FitTrack.Application.Abstractions.Persistence;

public record AdminCounts(
    int Exercises, int NewExercises,
    int Routines, int NewRoutines,
    int PublishedArticles, int NewArticles,
    int Users, int NewUsers);

public record RecentActivity(
    List<Exercise> Exercises,
    List<Article> Articles,
    List<User> Users,
    List<WorkoutLog> WorkoutLogs);

public interface IAdminStatsQueries
{
    Task<AdminCounts> GetCountsAsync(DateTime newSince);
    Task<RecentActivity> GetRecentActivityAsync(int perKind);
}
