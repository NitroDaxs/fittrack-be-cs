using System.ComponentModel.DataAnnotations;

namespace FitTrack.Application.Dtos;

public record ChatTurn(string Role, string Content);

public record ChatRequest
{
    [Required(ErrorMessage = "Type a message.")]
    [StringLength(1000, ErrorMessage = "Keep the messages under 1000 characters.")]
    public string? Message { get; init; }

    [MaxLength(40, ErrorMessage = "This conversation is too long. Start a new chat.")]
    public List<ChatTurn> History { get; init; } = [];

    [RegularExpression("^(en|bg)$", ErrorMessage = "Choose en or bg.")]
    public string? Language { get; init; }
}

public record ChatCardsDto(List<ExerciseDto> Exercises, List<RoutineDto> Routines, List<ArticleDto> Articles);

public record ChatReplyDto(string Reply, ChatCardsDto Cards);