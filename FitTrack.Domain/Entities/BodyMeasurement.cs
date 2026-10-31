namespace FitTrack.Domain.Entities;

public class BodyMeasurement : Timestamped
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly Date { get; set; }
    public decimal? Weight { get; set; }
    public decimal? BodyFatPct { get; set; }
    public CustomMeasurements? CustomMeasurements { get; set; }
    public string? PhotoUrl { get; set; }
}

public class CustomMeasurements
{
    public decimal? Waist { get; set; }
    public decimal? Chest { get; set; }
    public decimal? Arms { get; set; }
    public decimal? Shoulders { get; set; }
    public decimal? Hips { get; set; }
    public decimal? Thighs { get; set; }
    public decimal? Calves { get; set; }
}