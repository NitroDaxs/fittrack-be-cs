using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/routines")]
public class AdminRoutinesController(AppDbContext db) : ControllerBase
{
    private static readonly Dictionary<string, Expression<Func<Routine, object>>> Sortable = new()
    {
        ["title"] = r => r.Title,
        ["goal"] = r => r.Goal,
        ["level"] = r => r.Level,
        ["days_per_week"] = r => r.DaysPerWeek,
        ["daysPerWeek"] = r => r.DaysPerWeek,
        ["created_at"] = r => r.CreatedAt,
    };
    
    [HttpGet]
    public async Task<PagedResult<RoutineDto>> Index([FromQuery] AdminListQuery query)
    {
        IQueryable<Routine> routines = db.Routines.AsNoTracking().Include(r => r.Creator);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            routines = routines.Where(r => EF.Functions.ILike(r.Title, pattern) || EF.Functions.ILike(r.Description ?? "", pattern));
        }
        if (EnumValues.TryParse<RoutineGoal>(query.Goal, out var goal))
        {
            routines = routines.Where(r => r.Goal == goal);
        }
        if (EnumValues.TryParse<Difficulty>(query.Level, out var level))
        {
            routines = routines.Where(r => r.Level == level);
        }

        return await routines
            .SortBy(query.Sort, query.Dir, Sortable, fallback: "created_at")
            .PaginateAsync(query, defaultPerPage: 20, r => r.ToDto());
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoutineDto>> Show(int id)
    {
        var routine = await db.Routines.AsNoTracking().WithProgramme().FirstOrDefaultAsync(r => r.Id == id);
        return routine is null ? NotFound(new { message = "Routine not found." }) : routine.ToDto(withProgramme: true);
    }
    
    [HttpPost]
    public async Task<IActionResult> Store(RoutineRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) ModelState.AddModelError("title", "The title field is required.");
        if (request.Goal is null) ModelState.AddModelError("goal", "The goal field is required.");
        if (request.Level is null) ModelState.AddModelError("level", "The level field is required.");
        if (request.DaysPerWeek is null) ModelState.AddModelError("days_per_week", "The days per week field is required.");
        if (!ModelState.IsValid) return ApiErrors.Validation(ModelState);

        var routine = new Routine
        {
            Title = request.Title!,
            Slug = Slug.From(request.Title!),
            Description = request.Description,
            Goal = request.Goal!.Value,
            Level = request.Level!.Value,
            DaysPerWeek = request.DaysPerWeek!.Value,
            IsPublic = request.IsPublic ?? true,
            CreatedBy = User.GetUserId(),
        };

        if (await db.Routines.AnyAsync(r => r.Slug == routine.Slug))
        {
            return ApiErrors.Field("title", "A routine with this title already exists.");
        }

        if (request.Days is not null)
        {
            var error = await ReplaceDaysAsync(routine, request.Days);
            if (error is not null) return error;
        }

        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await LoadDtoAsync(routine.Id));
    }
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RoutineRequest request)
    {
        var routine = await db.Routines.Include(r => r.Workouts).FirstOrDefaultAsync(r => r.Id == id);
        if (routine is null)
        {
            return NotFound(new { message = "Routine not found." });
        }

        if (request.Title is not null)
        {
            var slug = Slug.From(request.Title);
            if (await db.Routines.AnyAsync(r => r.Slug == slug && r.Id != id))
            {
                return ApiErrors.Field("title", "A routine with this title already exists.");
            }

            routine.Title = request.Title;
            routine.Slug = slug;
        }
        routine.Description = request.Description ?? routine.Description;
        routine.Goal = request.Goal ?? routine.Goal;
        routine.Level = request.Level ?? routine.Level;
        routine.DaysPerWeek = request.DaysPerWeek ?? routine.DaysPerWeek;
        routine.IsPublic = request.IsPublic ?? routine.IsPublic;
        
        if (request.Days is not null)
        {
            var error = await ReplaceDaysAsync(routine, request.Days);
            if (error is not null) return error;
        }

        await db.SaveChangesAsync();
        return Ok(await LoadDtoAsync(id));
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Destroy(int id)
    {
        var deleted = await db.Routines.Where(r => r.Id == id).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound(new { message = "Routine not found." }) : Ok(new { status = "deleted" });
    }

    private async Task<IActionResult?> ReplaceDaysAsync(Routine routine, List<RoutineDayRequest> days)
    {
        var exerciseIds = days.SelectMany(d => d.Exercises).Select(e => e.ExerciseId!.Value).Distinct().ToList();
        var knownIds = await db.Exercises.Where(e => exerciseIds.Contains(e.Id)).Select(e => e.Id).ToListAsync();
        var unknownIds = exerciseIds.Except(knownIds).ToList();
        if (unknownIds.Count > 0)
        {
            return ApiErrors.Field("days", $"Unknown exercise id(s): {string.Join(", ", unknownIds)}.");
        }

        var userId = User.GetUserId();
        // RoutineId is nullable, so Clear() alone would leave the old days behind as orphans.
        db.Workouts.RemoveRange(routine.Workouts);
        routine.Workouts.Clear();
        routine.Workouts.AddRange(days.Select((day, dayIndex) => new Workout
        {
            Name = day.Name,
            DayOrder = dayIndex + 1,
            CreatedBy = userId,
            WorkoutExercises = day.Exercises.Select((entry, i) => new WorkoutExercise
            {
                ExerciseId = entry.ExerciseId!.Value,
                Order = i + 1,
                TargetSets = entry.TargetSets,
                TargetReps = entry.TargetReps,
                TargetWeight = entry.TargetWeight,
                RestSeconds = entry.RestSeconds,
                Notes = entry.Notes,
            }).ToList(),
        }));

        return null;
    }

    private async Task<RoutineDto> LoadDtoAsync(int id) =>
        (await db.Routines.AsNoTracking().WithProgramme().FirstAsync(r => r.Id == id)).ToDto(withProgramme: true);
}