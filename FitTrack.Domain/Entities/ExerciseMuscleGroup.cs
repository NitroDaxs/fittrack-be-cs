namespace FitTrack.Domain.Entities;

public class ExerciseMuscleGroup
{
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int MuscleGroupId { get; set; }
    public MuscleGroup MuscleGroup { get; set; } = null!;

    public MuscleRole Role { get; set; } = MuscleRole.Primary;
}