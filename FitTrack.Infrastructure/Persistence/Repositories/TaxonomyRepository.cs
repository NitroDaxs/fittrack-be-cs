namespace FitTrack.Infrastructure.Persistence.Repositories;

public class TaxonomyRepository(AppDbContext db) : ITaxonomyRepository
{
    public Task<List<TaxonomyDto>> GetMuscleGroupsAsync() =>
        db.MuscleGroups.OrderBy(m => m.Name).Select(m => new TaxonomyDto(m.Id, m.Name, m.Slug)).ToListAsync();

    public Task<List<TaxonomyDto>> GetEquipmentAsync() =>
        db.Equipment.OrderBy(e => e.Name).Select(e => new TaxonomyDto(e.Id, e.Name, e.Slug)).ToListAsync();

    public Task<List<string>> GetArticleAuthorsAsync() =>
        db.Articles.Select(a => a.Author.Name).Distinct().OrderBy(name => name).ToListAsync();

    public Task<List<MuscleGroup>> GetMuscleGroupsBySlugAsync(List<string> slugs) =>
        db.MuscleGroups.Where(m => slugs.Contains(m.Slug)).ToListAsync();

    public Task<List<int>> GetEquipmentIdsBySlugAsync(List<string> slugs) =>
        db.Equipment.Where(e => slugs.Contains(e.Slug)).Select(e => e.Id).ToListAsync();
}
