using System.Text.Json;
using FitTrack.Application.Services;

namespace FitTrack.Application.Chat.Tools;

public class ShowCardsTool(ExerciseService exercises, RoutineService routines, ArticleService articles) : IChatTool
{
    public const int MaxCards = 4;

    public string Name => "show_cards";

    public string Description =>
        "Show exercises, routines or articles as clickable cards under your reply. " +
        "Call it whenever you recommend specific items, with ids from the other tools.";

    public JsonElement Parameters { get; } = ToolJson.Schema("""
                                                             {
                                                               "type": "object",
                                                               "properties": {
                                                                 "items": {
                                                                   "type": "array",
                                                                   "maxItems": 4,
                                                                   "items": {
                                                                     "type": "object",
                                                                     "properties": {
                                                                       "type": { "type": "string", "enum": ["exercise", "routine", "article"] },
                                                                       "id": { "type": "integer" }
                                                                     },
                                                                     "required": ["type", "id"]
                                                                   }
                                                                 }
                                                               },
                                                               "required": ["items"]
                                                             }
                                                             """);

    public async Task<object> RunAsync(ToolArguments args, ToolContext context)
    {
        var shown = new List<string>();
        var notFound = new List<string>();

        foreach (var item in args.Objects("items"))
        {
            if (context.Cards.Count >= MaxCards) break;

            var type = item.String("type");
            var key = item.Int("id")?.ToString();
            if (key is null) continue;

            try
            {
                switch (type)
                {
                    case "exercise":
                        var exercise = await exercises.GetAsync(key);
                        context.Cards.Add(exercise);
                        shown.Add(exercise.Name);
                        break;
                    case "routine":
                        // The card only needs the summary, not the whole programme.
                        var routine = await routines.GetAsync(key) with { Workouts = null };
                        context.Cards.Add(routine);
                        shown.Add(routine.Title);
                        break;
                    case "article":
                        var article = await articles.GetAsync(key) with { Body = null, Related = null };
                        context.Cards.Add(article);
                        shown.Add(article.Title);
                        break;
                    default:
                        notFound.Add($"{type} {key}");
                        break;
                }
            }
            catch (NotFoundException)
            {
                notFound.Add($"{type} {key}");
            }
        }

        return new { Shown = shown, NotFound = notFound };
    }
}