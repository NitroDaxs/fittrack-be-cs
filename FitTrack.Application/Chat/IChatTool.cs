using System.Text.Json;
using FitTrack.Application.Chat;

namespace FitTrack.Application.Chat;

public interface IChatTool
{
    string Name { get; }

    string Description { get; }

    JsonElement Parameters { get; }

    Task<object> RunAsync(ToolArguments args, ToolContext context);
}

public record ToolContext(int UserId, ChatCards Cards);

public class ToolArgumentException(string message) : Exception(message);

public class ChatCards
{
    private readonly List<ExerciseDto> _exercises = [];
    private readonly List<RoutineDto> _routines = [];
    private readonly List<ArticleDto> _articles = [];

    public int Count => _exercises.Count + _routines.Count + _articles.Count;

    public void Add(ExerciseDto exercise)
    {
        if (_exercises.All(e => e.Id != exercise.Id)) _exercises.Add(exercise);
    }

    public void Add(RoutineDto routine)
    {
        if (_routines.All(r => r.Id != routine.Id)) _routines.Add(routine);
    }

    public void Add(ArticleDto article)
    {
        if (_articles.All(a => a.Id != article.Id)) _articles.Add(article);
    }

    public ChatCardsDto ToDto() => new([.. _exercises], [.. _routines], [.. _articles]);
}