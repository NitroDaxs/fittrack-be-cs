namespace FitTrack.Application.Services;

public class TaxonomyService(ITaxonomyRepository taxonomies)
{
    public async Task<TaxonomiesDto> GetAsync() => new(
        await taxonomies.GetMuscleGroupsAsync(),
        await taxonomies.GetEquipmentAsync(),
        EnumValues.Options<Difficulty>(),
        EnumValues.Options<ExerciseCategory>(),
        EnumValues.Options<ArticleCategory>(),
        EnumValues.Options<RoutineGoal>(),
        await taxonomies.GetArticleAuthorsAsync());
}
