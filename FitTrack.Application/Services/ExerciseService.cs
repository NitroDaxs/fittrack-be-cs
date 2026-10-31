namespace FitTrack.Application.Services;

public class ExerciseService(IExerciseRepository exercises)
{
    public async Task<PagedResult<ExerciseDto>> GetPageAsync(ExerciseSearch search, PageRequest paging)
    {
        var filter = new ExerciseFilter(
            search.Search,
            EnumValues.TryParse<Difficulty>(search.Difficulty, out var difficulty) ? difficulty : null,
            EnumValues.TryParse<ExerciseCategory>(search.ExerciseType, out var type) ? type : null,
            search.MuscleGroup,
            search.Equipment);

        var page = await exercises.GetPublishedPageAsync(filter, paging, defaultPerPage: 8);
        return page.Map(e => e.ToDto());
    }

    public async Task<ExerciseDto> GetAsync(string key)
    {
        var exercise = await exercises.FindPublishedAsync(key)
                       ?? throw new NotFoundException("Exercise not found");

        return exercise.ToDto();
    }
}
