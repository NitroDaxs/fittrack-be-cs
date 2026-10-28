using System.Text.Json.Serialization;

namespace FitTrack.Api.Entities;

public class BodyMeasurement : Timestamped
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateOnly Date { get; set; }
    public decimal? Weight { get; set; }
    public decimal? BodyFatPct  {get; set;}
    public CustomMeasurements? CustomMeasurements { get; set; }
    public string? PhotoUrl {get; set;}
}

public class CustomMeasurements
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Waist { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Chest { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Arms { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Shoulders { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Hips { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Thighs { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Calves { get; set; }
}