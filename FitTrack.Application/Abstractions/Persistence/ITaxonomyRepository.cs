namespace FitTrack.Application.Abstractions.Persistence;

public interface ITaxonomyRepository
{
    Task<List<TaxonomyDto>> GetMuscleGroupsAsync();
    Task<List<TaxonomyDto>> GetEquipmentAsync();
    Task<List<string>> GetArticleAuthorsAsync();
    Task<List<MuscleGroup>> GetMuscleGroupsBySlugAsync(List<string> slugs);
    Task<List<int>> GetEquipmentIdsBySlugAsync(List<string> slugs);
}
