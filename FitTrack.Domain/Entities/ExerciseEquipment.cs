namespace FitTrack.Domain.Entities;

public class ExerciseEquipment
{
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
}