namespace FitTrack.Api.Entities;

public class WorkoutLog : Timestamped
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? WorkoutId { get; set; }
    public Workout? Workout { get; set; }

    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }

    public List<SetLog> SetLogs { get; set; } = [];
}