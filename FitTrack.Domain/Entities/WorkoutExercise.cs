namespace FitTrack.Domain.Entities;

public class WorkoutExercise : Timestamped
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    
    public int Order { get; set; }
    public int? TargetSets { get; set; }
    public string? TargetReps { get; set; }
    public decimal? TargetWeight { get; set; }
    public int? RestSeconds { get; set; }
    public string? Notes { get; set; }
}