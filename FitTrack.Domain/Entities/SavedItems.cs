namespace FitTrack.Domain.Entities;

public class SavedExercise
{
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}

public class SavedRoutine
{
    public int RoutineId { get; set; }
    public Routine Routine { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}