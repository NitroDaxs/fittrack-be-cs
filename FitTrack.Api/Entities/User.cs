namespace FitTrack.Api.Entities;

public class User : Timestamped
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Member;
    public Units Units { get; set; } = Units.Metric;
    public bool IsActive { get; set; } = true;

    public List<WorkoutLog> WorkoutLogs { get; set; } = [];
    public List<BodyMeasurement> BodyMeasurements { get; set; } = [];
}