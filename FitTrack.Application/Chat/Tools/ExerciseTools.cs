using System.Text.Json;
using FitTrack.Application.Services;

namespace FitTrack.Application.Chat.Tools;

public class SearchExercisesTool(ExerciseService exercises) : IChatTool
{
    public string Name => "search_exercises";

    public string Description =>
        "Search FitTrack's exercise library. Combine a short keyword with filters. " +
        "Returns a short summary of each match; call get_exercise for technique and steps.";

    public JsonElement Parameters { get; } = ToolJson.Schema("""
                                                             {
                                                               "type": "object",
                                                               "properties": {
                                                                 "query": { "type": "string", "description": "One or two keywords from the exercise name, e.g. \"press\" or \"lunge\". Leave out to filter only." },
                                                                 "muscle_groups": { "type": "array", "items": { "type": "string" }, "description": "Muscle group names from the list in your instructions. Matches exercises that train any of them." },
                                                                 "equipment": { "type": "array", "items": { "type": "string" }, "description": "Equipment names from the list in your instructions. Matches exercises that use any of them." },
                                                                 "difficulty": { "type": "string", "enum": ["beginner", "intermediate", "advanced"] },
                                                                 "exercise_type": { "type": "string", "enum": ["strength", "cardio", "stretching", "plyometrics", "powerlifting", "olympic_weightlifting", "strongman"] },
                                                                 "limit": { "type": "integer", "minimum": 1, "maximum": 10, "description": "How many results to return. Default 6." }
                                                               }
                                                             }
                                                             """);

    public async Task<object> RunAsync(ToolArguments args, ToolContext context)
    {
        var search = new ExerciseSearch(
            args.String("query"),
            args.String("difficulty"),
            args.String("exercise_type"),
            args.Strings("muscle_groups"),
            args.Strings("equipment"));

        var page = await exercises.GetPageAsync(search, new PageRequest(1, Math.Clamp(args.Int("limit") ?? 6, 1, 10)));

        return new
        {
            page.Total,
            Exercises = page.Data.Select(e => new
            {
                e.Id,
                e.Name,
                Difficulty = e.Difficulty.ToValue(),
                Type = e.ExerciseType.ToValue(),
                PrimaryMuscles = e.MuscleGroups.Where(m => m.Pivot.Role == MuscleRole.Primary).Select(m => m.Name),
                Equipment = e.Equipment.Select(x => x.Name),
            }),
        };
    }
}

public class GetExerciseTool(ExerciseService exercises) : IChatTool
{
    public string Name => "get_exercise";

    public string Description =>
        "Get one exercise's full details: description, step-by-step instructions, muscles and equipment. " +
        "Use it before explaining how to perform an exercise.";

    public JsonElement Parameters { get; } = ToolJson.Schema("""
                                                             {
                                                               "type": "object",
                                                               "properties": {
                                                                 "id": { "type": "integer", "description": "The exercise id from search_exercises." }
                                                               },
                                                               "required": ["id"]
                                                             }
                                                             """);

    public async Task<object> RunAsync(ToolArguments args, ToolContext context)
    {
        var id = args.Int("id") ?? throw new ToolArgumentException("Pass the exercise id from search_exercises.");
        var e = await exercises.GetAsync(id.ToString());

        return new
        {
            e.Id,
            e.Name,
            e.Description,
            Difficulty = e.Difficulty.ToValue(),
            Type = e.ExerciseType.ToValue(),
            PrimaryMuscles = e.MuscleGroups.Where(m => m.Pivot.Role == MuscleRole.Primary).Select(m => m.Name),
            SecondaryMuscles = e.MuscleGroups.Where(m => m.Pivot.Role == MuscleRole.Secondary).Select(m => m.Name),
            Equipment = e.Equipment.Select(x => x.Name),
            Steps = (e.Instructions ?? "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            HasVideo = e.VideoUrl is not null,
        };
    }
}