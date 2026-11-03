using System.Text.Json;
using FitTrack.Application.Services;

namespace FitTrack.Application.Chat.Tools;

public class SearchRoutinesTool(RoutineService routines) : IChatTool
{
    public string Name => "search_routines";

    public string Description =>
        "Search FitTrack's ready-made training routines (multi-day programmes) by goal, level and days per week. " +
        "Call get_routine to see a routine's days and exercises.";

    public JsonElement Parameters { get; } = ToolJson.Schema("""
                                                             {
                                                               "type": "object",
                                                               "properties": {
                                                                 "query": { "type": "string", "description": "A keyword from the routine's title or description, e.g. \"upper\". Usually leave out." },
                                                                 "goals": { "type": "array", "items": { "type": "string", "enum": ["strength", "hypertrophy", "fat_loss", "endurance", "general_fitness"] } },
                                                                 "levels": { "type": "array", "items": { "type": "string", "enum": ["beginner", "intermediate", "advanced"] } },
                                                                 "days_per_week": { "type": "integer", "minimum": 1, "maximum": 7, "description": "Training days per week. 5 or more matches every routine with at least 5 days." },
                                                                 "limit": { "type": "integer", "minimum": 1, "maximum": 10, "description": "Default 6." }
                                                               }
                                                             }
                                                             """);

    public async Task<object> RunAsync(ToolArguments args, ToolContext context)
    {
        var days = args.Int("days_per_week") is { } d ? (d >= 5 ? "5" : d.ToString()) : null;
        var search = new RoutineSearch(args.String("query"), args.Strings("goals"), args.Strings("levels"), days);

        var page = await routines.GetPageAsync(search, new PageRequest(1, Math.Clamp(args.Int("limit") ?? 6, 1, 10)));

        return new
        {
            page.Total,
            Routines = page.Data.Select(r => new
            {
                r.Id,
                r.Title,
                Goal = r.Goal.ToValue(),
                Level = r.Level.ToValue(),
                r.DaysPerWeek,
                Description = Shorten(r.Description, 200),
            }),
        };
    }

    internal static string? Shorten(string? text, int length) =>
        text is null || text.Length <= length ? text : text[..length].TrimEnd() + "...";
}

public class GetRoutineTool(RoutineService routines) : IChatTool
{
    public string Name => "get_routine";

    public string Description =>
        "Get one routine's full programme: each training day with its exercises, sets, reps and rest.";

    public JsonElement Parameters { get; } = ToolJson.Schema("""
                                                             {
                                                               "type": "object",
                                                               "properties": {
                                                                 "id": { "type": "integer", "description": "The routine id from search_routines." }
                                                               },
                                                               "required": ["id"]
                                                             }
                                                             """);

    public async Task<object> RunAsync(ToolArguments args, ToolContext context)
    {
        var id = args.Int("id") ?? throw new ToolArgumentException("Pass the routine id from search_routines.");
        var r = await routines.GetAsync(id.ToString());

        return new
        {
            r.Id,
            r.Title,
            r.Description,
            Goal = r.Goal.ToValue(),
            Level = r.Level.ToValue(),
            r.DaysPerWeek,
            Days = (r.Workouts ?? []).Select(day => new
            {
                Day = day.Name,
                Exercises = day.WorkoutExercises.Select(x => new
                {
                    ExerciseId = x.Exercise.Id,
                    x.Exercise.Name,
                    Sets = x.TargetSets,
                    Reps = x.TargetReps,
                    x.RestSeconds,
                    x.Notes,
                }),
            }),
        };
    }
}