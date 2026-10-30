using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api/taxonomies")]
public class TaxonomiesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<TaxonomiesDto> Index() => new(
        await db.MuscleGroups.OrderBy(m => m.Name).Select(m => new TaxonomyDto(m.Id, m.Name, m.Slug)).ToListAsync(),
        await db.Equipment.OrderBy(e => e.Name).Select(e => new TaxonomyDto(e.Id, e.Name, e.Slug)).ToListAsync(),
        EnumValues.Options<Difficulty>(),
        EnumValues.Options<ExerciseCategory>(),
        EnumValues.Options<ArticleCategory>(),
        EnumValues.Options<RoutineGoal>());
}