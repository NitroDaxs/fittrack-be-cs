namespace FitTrack.Api.Entities;

public class Routine : Timestamped
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public RoutineGoal Goal { get; set; }
    public Difficulty Level { get; set; }
    public int DaysPerWeek { get; set; }
    public bool IsPublic { get; set; }
    public bool IsPublished { get; set; } = true;

    public int CreatedBy {get; set; }
    public User Creator { get; set; } = null!;

    public List<Workout> Workouts { get; set; } = [];
}