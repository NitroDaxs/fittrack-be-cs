namespace FitTrack.Api.Entities;

public class ExerciseMedia : Timestamped
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public MediaType Type { get; set; }
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
}