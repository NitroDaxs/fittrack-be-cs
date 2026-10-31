using FitTrack.Domain.Entities;

namespace FitTrack.Application.Services;

public class CalculatorService
{
    private const double KgPerLb = 0.453592;
    private const double CmPerInch = 2.54;

    private static readonly (int Percent, int TypicalReps)[] ZoneTable =
        [(100, 1), (95, 2), (90, 4), (85, 6), (80, 8), (75, 10), (70, 12)];

    private const int AccurateRepLimit = 12;

    private static readonly Dictionary<ActivityLevel, double> ActivityMultipliers = new()
    {
        [ActivityLevel.Sedentary] = 1.2,
        [ActivityLevel.Light] = 1.375,
        [ActivityLevel.Moderate] = 1.55,
        [ActivityLevel.Heavy] = 1.725,
        [ActivityLevel.Athlete] = 1.9,
    };

    private const int CutDeficit = 500;
    private const int LeanBulkSurplus = 300;

    private static readonly Dictionary<MacroSplit, (double Protein, double Carbs, double Fat)> Splits = new()
    {
        [MacroSplit.Balanced] = (0.30, 0.40, 0.30),
        [MacroSplit.HighProtein] = (0.40, 0.30, 0.30),
        [MacroSplit.LowCarb] = (0.40, 0.20, 0.40),
        [MacroSplit.Keto] = (0.25, 0.05, 0.70),
    };

    private const int KcalPerGramProteinOrCarbs = 4;
    private const int KcalPerGramFat = 9;

    private static readonly (double Below, BodyFatCategory Category)[] MaleBodyFat =
    [
        (6, BodyFatCategory.EssentialFat), (14, BodyFatCategory.Athletic), (18, BodyFatCategory.Fitness),
        (25, BodyFatCategory.Average), (double.PositiveInfinity, BodyFatCategory.AboveAverage),
    ];

    private static readonly (double Below, BodyFatCategory Category)[] FemaleBodyFat =
    [
        (14, BodyFatCategory.EssentialFat), (21, BodyFatCategory.Athletic), (25, BodyFatCategory.Fitness),
        (32, BodyFatCategory.Average), (double.PositiveInfinity, BodyFatCategory.AboveAverage),
    ];

    /// <summary>Epley: 1RM = w * (1 + r/30). Exact at one rep.</summary>
    public OneRepMaxResult OneRepMax(OneRepMaxRequest request)
    {
        var weight = request.Weight!.Value;
        var reps = request.Reps!.Value;

        var oneRepMax = RoundToInt(reps <= 1 ? weight : weight * (1 + reps / 30.0));
        var zones = ZoneTable
            .Select(zone => new TrainingZone(zone.Percent, RoundToInt(oneRepMax * zone.Percent / 100.0), zone.TypicalReps))
            .ToList();

        return new OneRepMaxResult(oneRepMax, reps > AccurateRepLimit, zones);
    }

    /// <summary>BMI = kg / m^2.</summary>
    public BmiResult Bmi(BmiRequest request)
    {
        var meters = HeightInCm(request) / 100;
        var bmi = WeightInKg(request) / (meters * meters);

        return new BmiResult(Math.Round(bmi, 1, MidpointRounding.AwayFromZero), bmi switch
        {
            < 18.5 => BmiCategory.Underweight,
            < 25 => BmiCategory.NormalWeight,
            < 30 => BmiCategory.Overweight,
            _ => BmiCategory.Obese,
        });
    }

    /// <summary>BMR from Mifflin-St Jeor, scaled by activity level.</summary>
    public TdeeResult Tdee(TdeeRequest request)
    {
        var baseRate = 10 * WeightInKg(request) + 6.25 * HeightInCm(request) - 5 * request.Age!.Value;
        var bmr = RoundToInt(request.Sex == Sex.Male ? baseRate + 5 : baseRate - 161);
        var tdee = RoundToInt(bmr * ActivityMultipliers[request.Activity!.Value]);

        return new TdeeResult(tdee, bmr, tdee - bmr, tdee - CutDeficit, tdee + LeanBulkSurplus);
    }

    /// <summary>Grams per macro = (calories * share) / kcal-per-gram.</summary>
    public MacroResult Macros(MacroRequest request)
    {
        var calories = request.Calories!.Value;
        var split = Splits[request.Split!.Value];

        return new MacroResult(calories,
        [
            Target(Macro.Protein, split.Protein, KcalPerGramProteinOrCarbs),
            Target(Macro.Carbs, split.Carbs, KcalPerGramProteinOrCarbs),
            Target(Macro.Fat, split.Fat, KcalPerGramFat),
        ]);

        MacroTarget Target(Macro macro, double share, int kcalPerGram) =>
            new(macro, RoundToInt(share * 100), RoundToInt(calories * share / kcalPerGram));
    }

    /// <summary>The US Navy tape-measurement method. These constants are for centimetres.</summary>
    public BodyFatResult BodyFat(BodyFatRequest request)
    {
        var isFemale = request.Sex == Sex.Female;

        ValidationFailedException.ThrowIfAny(
            (isFemale && request.Hip is null, "hip", "Enter your hip measurement."));

        var height = HeightInCm(request);
        var neck = ToCm(request.Neck!.Value, request.Units);
        var waist = ToCm(request.Waist!.Value, request.Units);
        var hip = isFemale ? ToCm(request.Hip!.Value, request.Units) : 0;

        ValidationFailedException.ThrowIfAny(
            (waist <= neck, "waist", "Waist must be larger than neck."));

        var bodyFat = isFemale
            ? 495 / (1.29579 - 0.35004 * Math.Log10(waist + hip - neck) + 0.221 * Math.Log10(height)) - 450
            : 495 / (1.0324 - 0.19077 * Math.Log10(waist - neck) + 0.15456 * Math.Log10(height)) - 450;

        // The formula goes negative when the measurements can't belong to one person.
        ValidationFailedException.ThrowIfAny(
            (bodyFat <= 0, "waist", "These measurements don't give a valid estimate. Please check them."));

        var category = (isFemale ? FemaleBodyFat : MaleBodyFat).First(band => bodyFat < band.Below).Category;

        return new BodyFatResult(Math.Round(bodyFat, 1, MidpointRounding.AwayFromZero), category);
    }

    private static double WeightInKg(BmiRequest request) =>
        request.Units == Units.Metric ? request.Weight!.Value : request.Weight!.Value * KgPerLb;

    private static double ToCm(double length, Units? units) =>
        units == Units.Metric ? length : length * CmPerInch;

    private static double HeightInCm(BodyHeightRequest request)
    {
        if (request.Units == Units.Metric)
        {
            ValidationFailedException.ThrowIfAny(
                (request.HeightCm is null, "height_cm", "Enter your height."));

            return request.HeightCm!.Value;
        }

        var inches = (request.HeightFeet ?? 0) * 12 + (request.HeightInches ?? 0);
        ValidationFailedException.ThrowIfAny(
            (inches <= 0, "height_feet", "Enter your height."));

        return inches * CmPerInch;
    }

    private static int RoundToInt(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
