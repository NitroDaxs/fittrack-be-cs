namespace FitTrack.Application.Services;

public class FavoritesService(
    IFavoritesRepository favorites, IExerciseRepository exercises, IRoutineRepository routines, IUnitOfWork unitOfWork)
{
    public async Task SaveExerciseAsync(int exerciseId, int userId)
    {
        if (!await exercises.ExistsAsync(exerciseId))
        {
            throw new NotFoundException("Exercise not found.");
        }

        if (!await favorites.IsExerciseSavedAsync(exerciseId, userId))
        {
            favorites.AddExercise(exerciseId, userId);
            await unitOfWork.SaveChangesAsync();
        }
    }

    public Task UnsaveExerciseAsync(int exerciseId, int userId) => favorites.RemoveExerciseAsync(exerciseId, userId);

    public async Task SaveRoutineAsync(int routineId, int userId)
    {
        if (!await routines.ExistsAsync(routineId))
        {
            throw new NotFoundException("Routine not found.");
        }

        if (!await favorites.IsRoutineSavedAsync(routineId, userId))
        {
            favorites.AddRoutine(routineId, userId);
            await unitOfWork.SaveChangesAsync();
        }
    }

    public Task UnsaveRoutineAsync(int routineId, int userId) => favorites.RemoveRoutineAsync(routineId, userId);
}
