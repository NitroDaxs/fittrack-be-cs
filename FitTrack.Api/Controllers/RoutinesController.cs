using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api/routines")]
public class RoutinesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<RoutineDto>> Index([FromQuery] PageQuery paging, [FromQuery] string? search)
    {
        var routines = db.Routines.AsNoTracking()
            .Include(r => r.Creator)
            .Where(r => r.IsPublic && r.IsPublished);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"^{search.Trim()}$";
            routines = routines.Where(r => 
                EF.Functions.ILike(r.Title, pattern) || EF.Functions.ILike(r.Description ?? "", pattern));
        }

        var goals = ParseAll<RoutineGoal>(Request.QueryList("goals", "goals[]", "goal", "goal[]"));
        if (goals.Count > 0)
        {
            routines = routines.Where(r => goals.Contains(r.Goal));
        }

        var levels = ParseAll<Difficulty>(Request.QueryList("levels", "levels[]", "level", "level[]"));
        if (levels.Count > 0)
        {
            routines = routines.Where(r => levels.Contains(r.Level));
        }
        
        var days = Request.QueryList("daysPerWeek", "days_per_week", "days", "days[]").FirstOrDefault();
        if (days is "5" or "5+")
        {
            routines = routines.Where(r => r.DaysPerWeek >= 5);
        }
        else if (int.TryParse(days, out var exactDays))
        {
            routines = routines.Where(r => r.DaysPerWeek == exactDays);
        }

        return await routines.OrderBy(r => r.Id).PaginateAsync(paging, defaultPerPage: 8, r => r.ToDto());
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<RoutineDto>> Show(string key)
    {
        var id = int.TryParse(key, out var parsed) ? parsed : 0;
        
        var routine = await db.Routines.AsNoTracking().WithProgramme()
            .FirstOrDefaultAsync(r => r.IsPublic && r.IsPublished && (r.Id == id || r.Slug == key));

        return routine is null ? NotFound(new { message = "Routine not found." }) : routine.ToDto(withProgramme: true);
    }
    
    private static List<TEnum> ParseAll<TEnum>(IEnumerable<string> values) where TEnum : struct, Enum =>
        values
            .Select(v => EnumValues.TryParse<TEnum>(v, out var parsed) ? parsed : (TEnum?)null)
            .OfType<TEnum>()
            .ToList();
}