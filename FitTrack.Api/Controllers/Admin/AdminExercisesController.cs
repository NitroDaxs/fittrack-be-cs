using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/exercises")]
public class AdminExercisesController(AppDbContext db) : ControllerBase
{
    private static readonly Dictionary<string, Expression<Func<Exercise, object>>> Sortable = new()
    {
        ["name"] = e => e.Name,
        ["difficulty"] = e => e.Difficulty,
        ["exercise_type"] = e => e.ExerciseType,
        ["created_at"] = e => e.CreatedAt,
    };
    
    [HttpGet]
    public async Task<PagedResult<ExerciseDto>> Index([FromQuery] AdminListQuery query)
    {
        var exercises = db.Exercises.AsNoTracking().WithDetails();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            exercises = exercises.Where(e =>
                EF.Functions.ILike(e.Name, pattern) || EF.Functions.ILike(e.Description ?? "", pattern));
        }

        if (EnumValues.TryParse<ExerciseCategory>(query.Category, out var category))
        {
            exercises = exercises.Where(e => e.ExerciseType == category);
        }

        return await exercises
            .SortBy(query.Sort, query.Dir, Sortable, fallback: "created_at")
            .PaginateAsync(query, defaultPerPage: 20, e => e.ToDto());
    }
    
    [HttpPost]
    public async Task<IActionResult> Store(ExerciseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) ModelState.AddModelError("name", "The name field is required.");
        if (request.Difficulty is null) ModelState.AddModelError("difficulty", "The difficulty field is required.");
        if (request.ExerciseType is null) ModelState.AddModelError("exercise_type", "The exercise type field is required.");
        if (!ModelState.IsValid) return ApiErrors.Validation(ModelState);

        var exercise = new Exercise
        {
            Name = request.Name!,
            Slug = Slug.From(request.Name!),
            Description = request.Description,
            Instructions = request.Instructions,
            Difficulty = request.Difficulty!.Value,
            ExerciseType = request.ExerciseType!.Value,
            VideoUrl = request.VideoUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            IsPublished = request.IsPublished ?? true,
            CreatedBy = User.GetUserId(),
        };

        if (await db.Exercises.AnyAsync(e => e.Slug == exercise.Slug))
        {
            return ApiErrors.Field("name", "An exercise with this name already exists.");
        }

        await SyncRelationsAsync(exercise, request);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, (await LoadAsync(exercise.Id)).ToDto());
    }
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ExerciseRequest request)
    {
        var exercise = await db.Exercises
            .Include(e => e.MuscleGroups)
            .Include(e => e.Equipment)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise is null)
        {
            return NotFound(new { message = "Exercise not found." });
        }

        if (request.Name is not null)
        {
            exercise.Name = request.Name;
            exercise.Slug = Slug.From(request.Name);
        }
        exercise.Description = request.Description ?? exercise.Description;
        exercise.Instructions = request.Instructions ?? exercise.Instructions;
        exercise.Difficulty = request.Difficulty ?? exercise.Difficulty;
        exercise.ExerciseType = request.ExerciseType ?? exercise.ExerciseType;
        exercise.VideoUrl = request.VideoUrl ?? exercise.VideoUrl;
        exercise.ThumbnailUrl = request.ThumbnailUrl ?? exercise.ThumbnailUrl;
        exercise.IsPublished = request.IsPublished ?? exercise.IsPublished;

        await SyncRelationsAsync(exercise, request);
        await db.SaveChangesAsync();

        return Ok((await LoadAsync(id)).ToDto());
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Destroy(int id)
    {
        var deleted = await db.Exercises.Where(e => e.Id == id).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound(new { message = "Exercise not found." }) : Ok(new { status = "deleted" });
    }
    
    private async Task SyncRelationsAsync(Exercise exercise, ExerciseRequest request)
    {
        if (request.MuscleGroups is not null)
        {
            var slugs = request.MuscleGroups.Select(Slug.From).ToList();
            var primary = (request.PrimaryMuscles ?? []).Select(Slug.From).ToHashSet();
            var groups = await db.MuscleGroups.Where(m => slugs.Contains(m.Slug)).ToListAsync();

            var wanted = groups.ToDictionary(
                g => g.Id,
                g => primary.Contains(g.Slug) ? MuscleRole.Primary : MuscleRole.Secondary);

            exercise.MuscleGroups.RemoveAll(link => !wanted.ContainsKey(link.MuscleGroupId));
            foreach (var link in exercise.MuscleGroups)
            {
                link.Role = wanted[link.MuscleGroupId];
            }
            foreach (var (groupId, role) in wanted)
            {
                if (exercise.MuscleGroups.All(link => link.MuscleGroupId != groupId))
                {
                    exercise.MuscleGroups.Add(new ExerciseMuscleGroup { MuscleGroupId = groupId, Role = role });
                }
            }
        }

        if (request.Equipment is not null)
        {
            var slugs = request.Equipment.Select(Slug.From).ToList();
            var ids = await db.Equipment.Where(e => slugs.Contains(e.Slug)).Select(e => e.Id).ToListAsync();

            exercise.Equipment.RemoveAll(link => !ids.Contains(link.EquipmentId));
            foreach (var equipmentId in ids.Where(i => exercise.Equipment.All(link => link.EquipmentId != i)))
            {
                exercise.Equipment.Add(new ExerciseEquipment { EquipmentId = equipmentId });
            }
        }
    }

    private Task<Exercise> LoadAsync(int id) =>
        db.Exercises.AsNoTracking().WithDetails().FirstAsync(e => e.Id == id);
}