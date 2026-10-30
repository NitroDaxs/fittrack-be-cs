using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

public class ExercisesQuery : PageQuery
{
    public string? Search { get; set; }
    public string? Difficulty { get; set; }

    [FromQuery(Name = "exercise_type")] public string? ExerciseType { get; set; }

    public string? MuscleGroup { get; set; }
    public string? Equipment { get; set; }
}

[ApiController]
[Route("api/exercises")]
public class ExercisesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<ExerciseDto>> Index([FromQuery] ExercisesQuery query)
    {
        var exercises = db.Exercises.AsNoTracking().WithDetails().Where(e => e.IsPublished);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            exercises = exercises.Where(e =>
                EF.Functions.ILike(e.Name, pattern) || EF.Functions.ILike(e.Description ?? "", pattern));
        }

        if (EnumValues.TryParse<Difficulty>(query.Difficulty, out var difficulty))
        {
            exercises = exercises.Where(e => e.Difficulty == difficulty);
        }

        if (EnumValues.TryParse<ExerciseCategory>(query.ExerciseType, out var type))
        {
            exercises = exercises.Where(e => e.ExerciseType == type);
        }

        if (!string.IsNullOrWhiteSpace(query.MuscleGroup))
        {
            var key = query.MuscleGroup;
            var slug = key.ToLowerInvariant();
            var id = int.TryParse(key, out var parsed) ? parsed : 0;
            exercises = exercises.Where(e =>
                e.MuscleGroups.Any(l =>
                    l.MuscleGroupId == id || l.MuscleGroup.Slug == slug || l.MuscleGroup.Name == key));
        }

        if (!string.IsNullOrWhiteSpace(query.Equipment))
        {
            var key = query.Equipment;
            var slug = key.ToLowerInvariant();
            var id = int.TryParse(key, out var parsed) ? parsed : 0;
            exercises = exercises.Where(e => e.Equipment.Any(l =>
                l.EquipmentId == id || l.Equipment.Slug == slug || l.Equipment.Name == key));
        }

        return await exercises.OrderBy(e => e.Id).PaginateAsync(query, defaultPerPage: 8, e => e.ToDto());
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<ExerciseDto>> Show(string key)
    {
        var id = int.TryParse(key, out var parsed) ? parsed : 0;

        var exercise = await db.Exercises.AsNoTracking().WithDetails()
            .FirstOrDefaultAsync(e => e.IsPublished && (e.Id == id || e.Slug == key));

        return exercise is null ? NotFound(new { message = "Exercise not found" }) : exercise.ToDto();
    }
}