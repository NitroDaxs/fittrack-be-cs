namespace FitTrack.Api.Entities;

public abstract class Timestamped
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}