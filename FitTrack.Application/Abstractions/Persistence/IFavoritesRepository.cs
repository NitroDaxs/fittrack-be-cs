namespace FitTrack.Application.Abstractions.Persistence;

public interface IFavoritesRepository
{
    Task<bool> IsExerciseSavedAsync(int exerciseId, int userId);
    void AddExercise(int exerciseId, int userId);
    Task RemoveExerciseAsync(int exerciseId, int userId);
    Task<List<Exercise>> GetSavedExercisesAsync(int userId);

    Task<bool> IsRoutineSavedAsync(int routineId, int userId);
    void AddRoutine(int routineId, int userId);
    Task RemoveRoutineAsync(int routineId, int userId);
    Task<List<Routine>> GetSavedRoutinesAsync(int userId);
}
