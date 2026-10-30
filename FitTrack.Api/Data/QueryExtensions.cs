namespace FitTrack.Api.Data;

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
}