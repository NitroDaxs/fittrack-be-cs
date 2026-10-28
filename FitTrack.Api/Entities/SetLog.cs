namespace FitTrack.Api.Entities;

public class SetLog : Timestamped
{
    public int Id { get; set; }
    public int WorkoutLogId { get; set; }
    public WorkoutLog WorkoutLog { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    
    public int SetNumber { get; set; }
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public bool IsCompleted { get; set; } = true;
}