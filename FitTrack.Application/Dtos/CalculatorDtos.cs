using System.ComponentModel.DataAnnotations;
using FitTrack.Domain.Entities;

namespace FitTrack.Application.Dtos;

public enum Sex { Male, Female }

public enum ActivityLevel { Sedentary, Light, Moderate, Heavy, Athlete }

public enum MacroSplit { Balanced, HighProtein, LowCarb, Keto }

public enum Macro { Protein, Carbs, Fat }

public enum BmiCategory { Underweight, NormalWeight, Overweight, Obese }

public enum BodyFatCategory { EssentialFat, Athletic, Fitness, Average, AboveAverage }

// Values arrive in whichever units the person chose. The service converts them.

public record OneRepMaxRequest
{
    [Required(ErrorMessage = "Enter the weight you lifted.")]
    [Range(0.1, 5000, ErrorMessage = "Enter a weight greater than zero.")]
    public double? Weight { get; init; }

    [Required(ErrorMessage = "Enter the reps you performed.")]
    [Range(1, 20, ErrorMessage = "Enter between 1 and 20 reps.")]
    public int? Reps { get; init; }
}

public record TrainingZone(int Percent, int Load, int TypicalReps);

public record OneRepMaxResult(int OneRepMax, bool LowAccuracy, List<TrainingZone> Zones);

public record BodyHeightRequest
{
    [Required(ErrorMessage = "Choose metric or imperial units.")]
    public Units? Units { get; init; }

    [Range(0, 10, ErrorMessage = "Enter between 0 and 10 feet.")]
    public double? HeightFeet { get; init; }

    [Range(0, 11.99, ErrorMessage = "Enter between 0 and 11.99 inches.")]
    public double? HeightInches { get; init; }

    [Range(1, 300, ErrorMessage = "Enter a height between 1 and 300 cm.")]
    public double? HeightCm { get; init; }
}

public record BmiRequest : BodyHeightRequest
{
    [Required(ErrorMessage = "Enter your weight.")]
    [Range(0.1, 2000, ErrorMessage = "Enter a weight greater than zero.")]
    public double? Weight { get; init; }
}

public record BmiResult(double Bmi, BmiCategory Category);

public record TdeeRequest : BmiRequest
{
    [Required(ErrorMessage = "Choose your sex.")]
    public Sex? Sex { get; init; }

    [Required(ErrorMessage = "Enter your age.")]
    [Range(1, 120, ErrorMessage = "Enter an age between 1 and 120.")]
    public int? Age { get; init; }

    [Required(ErrorMessage = "Choose your activity level.")]
    public ActivityLevel? Activity { get; init; }
}

public record TdeeResult(int Tdee, int Bmr, int ActivityCalories, int Cut, int LeanBulk);

public record MacroRequest
{
    [Required(ErrorMessage = "Enter your daily calories.")]
    [Range(1, 20000, ErrorMessage = "Enter between 1 and 20,000 calories.")]
    public int? Calories { get; init; }

    [Required(ErrorMessage = "Choose a split.")]
    public MacroSplit? Split { get; init; }
}

public record MacroTarget(Macro Macro, int Percent, int Grams);

public record MacroResult(int Calories, List<MacroTarget> Targets);

public record BodyFatRequest : BodyHeightRequest
{
    [Required(ErrorMessage = "Choose your sex.")]
    public Sex? Sex { get; init; }

    [Required(ErrorMessage = "Enter your neck measurement.")]
    [Range(0.1, 500, ErrorMessage = "Enter a measurement greater than zero.")]
    public double? Neck { get; init; }

    [Required(ErrorMessage = "Enter your waist measurement.")]
    [Range(0.1, 500, ErrorMessage = "Enter a measurement greater than zero.")]
    public double? Waist { get; init; }

    [Range(0.1, 500, ErrorMessage = "Enter a measurement greater than zero.")]
    public double? Hip { get; init; }
}

public record BodyFatResult(double BodyFat, BodyFatCategory Category);
