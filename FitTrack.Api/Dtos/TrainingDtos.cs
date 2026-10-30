using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FitTrack.Api.Dtos;

public record SetLogDto(
    int Id,
    int ExerciseId,
    int SetNumber,
    decimal Weight,
    int Reps,
    bool IsCompleted,
    ExerciseDto Exercise);

public record WorkoutLogDto(
    int Id,
    int UserId,
    int? WorkoutId,
    string Name,
    DateTime Date,
    int? DurationMinutes,
    string? Notes,
    DateTime CreatedAt,
    List<SetLogDto> SetLogs);

public record LogWorkoutRequest
{
    [Required, MaxLength(255)] public string Name { get; init; } = "";
    public int? WorkoutId { get; init; }
    [Range(0, 1440)] public int? DurationMinutes { get; init; }
    public DateTime? Date { get; init; }
    public string? Notes { get; init; }
    [Required, MinLength(1)] public List<LoggedExercise> Exercises { get; init; } = [];
}

public record LoggedExercise
{
    [Required] public int? ExerciseId { get; init; }
    [Required, MinLength(1)] public List<LoggedSet> Sets { get; init; } = [];
}

public record LoggedSet
{
    [Range(0, 10000)] public decimal? Weight { get; init; }
    [Range(0, 1000)] public int? Reps { get; init; }
}

public record BodyMeasurementDto(
    int Id,
    DateOnly Date,
    decimal? Weight,
    decimal? BodyFatPct,
    CustomMeasurements? CustomMeasurements,
    string? PhotoUrl,
    DateTime CreatedAt);

public record LogMeasurementRequest
{
    public DateOnly? Date { get; init; }
    [Range(0, 999)] public decimal? Weight { get; init; }
    [Range(0, 100)] public decimal? BodyFatPct { get; init; }
    public CustomMeasurements? CustomMeasurements { get; init; }
    public string? PhotoUrl { get; init; }
}

public record DashboardDto(
    DashboardUser User,
    DashboardStats Stats,
    List<DayCount> Consistency,
    List<TrendPoint> MeasurementTrend,
    ProgressionDto? DefaultProgression);

public record DashboardUser(int Id, string Name);

public record DashboardStats(int CurrentStreak, int WorkoutsThisWeek, decimal? LatestBodyWeight);

public record DayCount(DateOnly Date, int Count);

public record TrendPoint(DateOnly Date, decimal? Weight, decimal? BodyFatPct);

public record ProgressionPoint(
    DateOnly Date,
    decimal MaxWeight,
    [property: JsonPropertyName("estimated_1rm")]
    decimal Estimated1Rm);

public record ProgressionDto(CreatorDto Exercise, List<ProgressionPoint> Data);