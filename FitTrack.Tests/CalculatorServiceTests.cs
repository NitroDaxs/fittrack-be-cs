using FitTrack.Application.Common;
using FitTrack.Application.Dtos;
using FitTrack.Application.Services;
using FitTrack.Domain.Entities;

namespace FitTrack.Tests;

public class CalculatorServiceTests
{
    private readonly CalculatorService _calculators = new();

    [Fact]
    public void One_rep_max_uses_epley_and_builds_zones()
    {
        var result = _calculators.OneRepMax(new OneRepMaxRequest { Weight = 100, Reps = 5 });

        Assert.Equal(117, result.OneRepMax);
        Assert.False(result.LowAccuracy);
        Assert.Equal(7, result.Zones.Count);
        Assert.Equal(new TrainingZone(100, 117, 1), result.Zones[0]);
        Assert.Equal(new TrainingZone(70, 82, 12), result.Zones[6]);
    }

    [Fact]
    public void One_rep_max_of_a_single_is_the_weight_itself()
    {
        Assert.Equal(100, _calculators.OneRepMax(new OneRepMaxRequest { Weight = 100, Reps = 1 }).OneRepMax);
    }

    [Theory]
    [InlineData(12, false)]
    [InlineData(13, true)]
    public void One_rep_max_flags_high_reps_as_less_accurate(int reps, bool lowAccuracy)
    {
        Assert.Equal(lowAccuracy, _calculators.OneRepMax(new OneRepMaxRequest { Weight = 100, Reps = reps }).LowAccuracy);
    }

    [Fact]
    public void Bmi_from_imperial_inputs()
    {
        var result = _calculators.Bmi(new BmiRequest { Units = Units.Imperial, Weight = 185, HeightFeet = 5, HeightInches = 10 });

        Assert.Equal(26.5, result.Bmi);
        Assert.Equal(BmiCategory.Overweight, result.Category);
    }

    [Fact]
    public void Bmi_from_metric_inputs()
    {
        var result = _calculators.Bmi(new BmiRequest { Units = Units.Metric, Weight = 80, HeightCm = 180 });

        Assert.Equal(24.7, result.Bmi);
        Assert.Equal(BmiCategory.NormalWeight, result.Category);
    }

    [Fact]
    public void Height_is_required_for_the_chosen_units()
    {
        var metric = Assert.Throws<ValidationFailedException>(() =>
            _calculators.Bmi(new BmiRequest { Units = Units.Metric, Weight = 80, HeightFeet = 5 }));
        Assert.Contains("height_cm", metric.Errors.Keys);

        var imperial = Assert.Throws<ValidationFailedException>(() =>
            _calculators.Bmi(new BmiRequest { Units = Units.Imperial, Weight = 180, HeightCm = 180 }));
        Assert.Contains("height_feet", imperial.Errors.Keys);
    }

    [Fact]
    public void Tdee_for_a_moderately_active_man()
    {
        var result = _calculators.Tdee(new TdeeRequest
        {
            Units = Units.Imperial, Sex = Sex.Male, Age = 30, Weight = 185, HeightFeet = 5, HeightInches = 10,
            Activity = ActivityLevel.Moderate,
        });

        Assert.Equal(new TdeeResult(2798, 1805, 993, 2298, 3098), result);
    }

    [Fact]
    public void Tdee_uses_the_lower_constant_for_women()
    {
        var result = _calculators.Tdee(new TdeeRequest
        {
            Units = Units.Metric, Sex = Sex.Female, Age = 30, Weight = 60, HeightCm = 165,
            Activity = ActivityLevel.Sedentary,
        });

        Assert.Equal(1320, result.Bmr);
        Assert.Equal(1584, result.Tdee);
    }

    [Fact]
    public void Macros_convert_shares_to_grams()
    {
        var result = _calculators.Macros(new MacroRequest { Calories = 2400, Split = MacroSplit.Balanced });

        Assert.Equal(
            [new MacroTarget(Macro.Protein, 30, 180), new MacroTarget(Macro.Carbs, 40, 240), new MacroTarget(Macro.Fat, 30, 80)],
            result.Targets);
    }

    [Fact]
    public void Keto_is_mostly_fat()
    {
        var fat = _calculators.Macros(new MacroRequest { Calories = 2400, Split = MacroSplit.Keto }).Targets[2];

        Assert.Equal(new MacroTarget(Macro.Fat, 70, 187), fat);
    }

    [Fact]
    public void Body_fat_for_a_man_in_inches_and_in_centimetres_agree()
    {
        var imperial = _calculators.BodyFat(new BodyFatRequest
        {
            Units = Units.Imperial, Sex = Sex.Male, HeightFeet = 5, HeightInches = 10, Neck = 15, Waist = 34,
        });
        var metric = _calculators.BodyFat(new BodyFatRequest
        {
            Units = Units.Metric, Sex = Sex.Male, HeightCm = 177.8, Neck = 38.1, Waist = 86.36,
        });

        Assert.Equal(17.4, imperial.BodyFat);
        Assert.Equal(BodyFatCategory.Fitness, imperial.Category);
        Assert.Equal(imperial, metric);
    }

    [Fact]
    public void Body_fat_uses_hip_for_women_and_requires_it()
    {
        var result = _calculators.BodyFat(new BodyFatRequest
        {
            Units = Units.Imperial, Sex = Sex.Female, HeightFeet = 5, HeightInches = 5, Neck = 13, Waist = 30, Hip = 38,
        });
        Assert.Equal(28.3, result.BodyFat);
        Assert.Equal(BodyFatCategory.Average, result.Category);

        var missing = Assert.Throws<ValidationFailedException>(() => _calculators.BodyFat(new BodyFatRequest
        {
            Units = Units.Imperial, Sex = Sex.Female, HeightFeet = 5, HeightInches = 5, Neck = 13, Waist = 30,
        }));
        Assert.Contains("hip", missing.Errors.Keys);
    }

    [Fact]
    public void Body_fat_rejects_a_waist_no_larger_than_the_neck()
    {
        var error = Assert.Throws<ValidationFailedException>(() => _calculators.BodyFat(new BodyFatRequest
        {
            Units = Units.Imperial, Sex = Sex.Male, HeightFeet = 5, HeightInches = 10, Neck = 20, Waist = 20,
        }));

        Assert.Contains("waist", error.Errors.Keys);
    }
}
