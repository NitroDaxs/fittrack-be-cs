using System.Text;
using System.Text.Json;
using Fittrack.Application.Chat;
using FitTrack.Application.Services;

namespace FitTrack.Application.Chat.Tools;

public class SearchArticlesTool(ArticleService articles) : IChatTool
{
    public string Name => "search_articles";

    public string Description =>
        "Search FitTrack's articles on nutrition, training theory, recovery and mindset. " +
        "Returns titles and short excerpts; call read_article before answering from an article.";

    public JsonElement Parameters { get; } = ToolJson.Schema("""
        {
          "type": "object",
          "properties": {
            "query": { "type": "string", "description": "One or two keywords, e.g. \"protein\" or \"sleep\"." },
            "category": { "type": "string", "enum": ["nutrition", "training_theory", "recovery", "mindset"] },
            "limit": { "type": "integer", "minimum": 1, "maximum": 10, "description": "Default 5." }
          }
        }
        """);

    public async Task<object> RunAsync(ToolArguments args, ToolContext context)
    {
        var page = await articles.GetPageAsync(
            args.String("query"), args.String("category"), new PageRequest(1, Math.Clamp(args.Int("limit") ?? 5, 1, 10)));

        return new
        {
            page.Total,
            Articles = page.Data.Select(a => new
            {
                a.Id,
                a.Title,
                Category = a.Category.ToValue(),
                a.Excerpt,
                a.ReadMinutes,
            }),
        };
    }
}

public class ReadArticleTool(ArticleService articles) : IChatTool
{
    private const int MaxCharacters = 6000;

    public string Name => "read_article";

    public string Description =>
        "Read one article's full text. Use it to answer from the article or to summarise it.";

    public JsonElement Parameters { get; } = ToolJson.Schema("""
        {
          "type": "object",
          "properties": {
            "id": { "type": "integer", "description": "The article id from search_articles." }
          },
          "required": ["id"]
        }
        """);

    public async Task<object> RunAsync(ToolArguments args, ToolContext context)
    {
        var id = args.Int("id") ?? throw new ToolArgumentException("Pass the article id from search_articles.");
        var a = await articles.GetAsync(id.ToString());
        var text = PlainText(a.Body);

        return new
        {
            a.Id,
            a.Title,
            Category = a.Category.ToValue(),
            a.Author,
            a.Date,
            Text = text.Length <= MaxCharacters ? text : text[..MaxCharacters],
            Truncated = text.Length > MaxCharacters,
        };
    }

    private static string PlainText(JsonElement? blocks)
    {
        if (blocks is not { ValueKind: JsonValueKind.Array } array) return "";

        var text = new StringBuilder();
        foreach (var block in array.EnumerateArray())
        {
            if (block.ValueKind == JsonValueKind.Object
                && block.TryGetProperty("text", out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                text.Append(value.GetString()).Append("\n\n");
            }
        }

        return text.ToString().Trim();
    }
}