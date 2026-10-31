namespace FitTrack.Domain.Entities;

public class Workout : Timestamped
{
    public int Id { get; set; }
    public int? RoutineId { get; set; }
    public Routine? Routine { get; set; }
    public string Name { get; set; } = "";
    public int? DayOrder { get; set; }
    public bool IsTemplate { get; set; } = true;

    public int CreatedBy { get; set; }
    public User Creator { get; set; } = null!;

    public List<WorkoutExercise> WorkoutExercises { get; set; } = [];
}

