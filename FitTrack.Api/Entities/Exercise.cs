namespace FitTrack.Api.Entities;

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public Difficulty Difficulty { get; set; }
    public ExerciseCategory ExerciseType {get; set;}
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public User Creator { get; set; } = null!;

    public List<ExerciseMuscleGroup> MuscleGroups { get; set; } = [];
    public List<ExerciseEquipment> Equipments { get; set; } = [];
    public List<ExerciseMedia> Media { get; set; } = [];
}