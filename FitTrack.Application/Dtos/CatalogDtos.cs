using System.Text.Json;
using System.Text.Json.Serialization;

namespace FitTrack.Application.Dtos;

public record TaxonomyDto(int Id, string Name, string Slug);

public record TaxonomiesDto(
    List<TaxonomyDto> Muscles,
    List<TaxonomyDto> Equipment,
    List<OptionDto> Difficulties,
    List<OptionDto> Categories,
    [property: JsonPropertyName("articleCategories")]
    List<OptionDto> ArticleCategories,
    List<OptionDto> Goals,
    List<string> Authors);

public record PivotDto(MuscleRole Role);

public record MuscleGroupDto(int Id, string Name, string Slug, PivotDto Pivot);

public record MediaDto(int Id, MediaType Type, string Url, int SortOrder);

public record ExerciseDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? Instructions,
    Difficulty Difficulty,
    ExerciseCategory ExerciseType,
    string? VideoUrl,
    string? ThumbnailUrl,
    int CreatedBy,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<MuscleGroupDto> MuscleGroups,
    List<TaxonomyDto> Equipment,
    List<MediaDto> Media);

public record CreatorDto(int Id, string Name);

public record WorkoutExerciseDto(
    int Id,
    int ExerciseId,
    int Order,
    int? TargetSets,
    string? TargetReps,
    decimal? TargetWeight,
    int? RestSeconds,
    string? Notes,
    ExerciseDto Exercise);

public record WorkoutDto(int Id, string Name, int? DayOrder, List<WorkoutExerciseDto> WorkoutExercises);

public record RoutineDto(
    int Id,
    string Title,
    string Slug,
    string? Description,
    RoutineGoal Goal,
    Difficulty Level,
    int DaysPerWeek,
    bool IsPublic,
    bool IsPublished,
    DateTime CreatedAt,
    CreatorDto? Creator)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<WorkoutDto>? Workouts { get; init; }
}

public record ArticleDto(
    int Id,
    string Slug,
    string Title,
    ArticleCategory Category,
    ArticleStatus Status,
    string Excerpt,
    string Author,
    DateOnly Date,
    [property: JsonPropertyName("readMinutes")]
    int ReadMinutes,
    string Image)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Body { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ArticleDto>? Related { get; init; }
}

public record FavoritesDto(List<RoutineDto> Routines, List<ExerciseDto> Exercises);

public record ExerciseSearch(
    string? Search = null,
    string? Difficulty = null,
    string? ExerciseType = null,
    string? MuscleGroup = null,
    string? Equipment = null);

public record RoutineSearch(
    string? Search,
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> Levels,
    string? Days);
