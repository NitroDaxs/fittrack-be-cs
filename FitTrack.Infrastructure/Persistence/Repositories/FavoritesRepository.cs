namespace FitTrack.Infrastructure.Persistence.Repositories;

public class FavoritesRepository(AppDbContext db) : IFavoritesRepository
{
    public Task<bool> IsExerciseSavedAsync(int exerciseId, int userId) =>
        db.SavedExercises.AnyAsync(s => s.ExerciseId == exerciseId && s.UserId == userId);

    public void AddExercise(int exerciseId, int userId) =>
        db.SavedExercises.Add(new SavedExercise { ExerciseId = exerciseId, UserId = userId });

    public Task RemoveExerciseAsync(int exerciseId, int userId) =>
        db.SavedExercises.Where(s => s.ExerciseId == exerciseId && s.UserId == userId).ExecuteDeleteAsync();

    public Task<List<Exercise>> GetSavedExercisesAsync(int userId) =>
        db.Exercises.AsNoTracking().WithDetails()
            .Where(e => db.SavedExercises.Any(s => s.ExerciseId == e.Id && s.UserId == userId))
            .ToListAsync();

    public Task<bool> IsRoutineSavedAsync(int routineId, int userId) =>
        db.SavedRoutines.AnyAsync(s => s.RoutineId == routineId && s.UserId == userId);

    public void AddRoutine(int routineId, int userId) =>
        db.SavedRoutines.Add(new SavedRoutine { RoutineId = routineId, UserId = userId });

    public Task RemoveRoutineAsync(int routineId, int userId) =>
        db.SavedRoutines.Where(s => s.RoutineId == routineId && s.UserId == userId).ExecuteDeleteAsync();

    public Task<List<Routine>> GetSavedRoutinesAsync(int userId) =>
        db.Routines.AsNoTracking().Include(r => r.Creator)
            .Where(r => db.SavedRoutines.Any(s => s.RoutineId == r.Id && s.UserId == userId))
            .ToListAsync();
}
