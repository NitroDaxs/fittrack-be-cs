using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace FitTrack.Api.Dtos;

public class AdminListQuery : PageQuery
{
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public string? Dir { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public string? Goal { get; set; }
    public string? Level { get; set; }
}

public record ExerciseRequest
{
    [MaxLength(255)] public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Instructions { get; init; }
    public Difficulty? Difficulty { get; init; }
    public ExerciseCategory? ExerciseType { get; init; }
    [Url] public string? VideoUrl { get; init; }
    [Url] public string? ThumbnailUrl { get; init; }
    public bool? IsPublished { get; init; }
    public List<string>? MuscleGroups { get; init; }   // slugs or names
    public List<string>? PrimaryMuscles { get; init; }
    public List<string>? Equipment { get; init; }
}

public record RoutineRequest
{
    [MaxLength(255)] public string? Title { get; init; }
    public string? Description { get; init; }
    public RoutineGoal? Goal { get; init; }
    public Difficulty? Level { get; init; }
    [Range(1,7)] public int? DaysPerWeek { get; init; }
    public bool? IsPublic { get; init; }
    public List<RoutineDayRequest>? Days { get; init; }
}

public record RoutineDayRequest
{
    [Required, MaxLength(255)] public string Name { get; init; } = "";
    public List<RoutineDayExerciseRequest> Exercises { get; init; } = [];
}

public record RoutineDayExerciseRequest
{
    [Required] public int? ExerciseId { get; init; }
    [Range(1, 20)] public int? TargetSets { get; init; }
    [MaxLength(20)] public string? TargetReps { get; init; }
    [Range(0, 10000)] public decimal? TargetWeight { get; init; }
    [Range(0, 900)] public int? RestSeconds { get; init; }
    public string? Notes { get; init; }
}

public record ArticleRequest
{
    [MaxLength(255)] public string? Title { get; init; }
    public ArticleCategory? Category { get; init; }
    public ArticleStatus? Status { get; init; }
    public JsonElement? Body { get; init; }
    public string? Excerpt { get; init; }
}

public record AdminUserRequest
{
    [MaxLength(255)] public string? Name { get; init; }
    [EmailAddress] public string? Email { get; init; }
    [MinLength(8)] public string? Password { get; init; }
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}

public record AdminStatsDto(
    int TotalExercises, string ExerciseDelta,
    int TotalRoutines, string RoutineDelta,
    int PublishedArticles, string ArticleDelta,
    int TotalUsers, string UserDelta,
    int ApiUptime, int StorageCapacity);

public record ActivityDto(string Id, string Icon, string Tone, string Text, string Meta);