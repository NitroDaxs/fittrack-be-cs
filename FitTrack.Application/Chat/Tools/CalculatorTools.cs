using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Fittrack.Application.Chat;
using FitTrack.Application.Services;

namespace FitTrack.Application.Chat.Tools;

public class CalculatorTool<TRequest>(
    string name,
    string description,
    string schema,
    Func<TRequest, object> calculate) : IChatTool
{
    public string Name => name;
    public string Description => description;
    public JsonElement Parameters { get; } = ToolJson.Schema(schema);

    public Task<object> RunAsync(ToolArguments args, ToolContext context)
    {
        TRequest? request;
        try
        {
            request = args.Json.Deserialize<TRequest>(ToolJson.Options);
        }
        catch (JsonException)
        {
            throw new ToolArgumentException("One of the values has the wrong type or isn't an allowed option.");
        }

        if (request is null) throw new ToolArgumentException("No values were given.");

        var problems = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), problems,
                validateAllProperties: true))
        {
            throw new ToolArgumentException(
                "Ask the person for the missing or invalid values: " +
                string.Join(" ", problems.Select(p => p.ErrorMessage)));
        }

        return Task.FromResult(calculate(request));
    }
}

public class OneRepMaxTool(CalculatorService calculators) : CalculatorTool<OneRepMaxRequest>(
    "calculate_one_rep_max",
    "Estimate a one-rep max (1RM) from a weight lifted for a number of reps, plus training loads at 70-100% of it. " +
    "The result uses the same unit as the weight.",
    """
    {
      "type": "object",
      "properties": {
        "weight": { "type": "number", "description": "The weight lifted, in the person's unit." },
        "reps": { "type": "integer", "minimum": 1, "maximum": 20 }
      },
      "required": ["weight", "reps"]
    }
    """,
    request => calculators.OneRepMax(request));

public class BmiTool(CalculatorService calculators) : CalculatorTool<BmiRequest>(
    "calculate_bmi",
    "Calculate body mass index (BMI) and its category. Metric needs height_cm; imperial needs height_feet and height_inches.",
    """
    {
      "type": "object",
      "properties": {
        "units": { "type": "string", "enum": ["metric", "imperial"] },
        "weight": { "type": "number", "description": "kg for metric, lb for imperial." },
        "height_cm": { "type": "number" },
        "height_feet": { "type": "number" },
        "height_inches": { "type": "number" }
      },
      "required": ["units", "weight"]
    }
    """,
    request => calculators.Bmi(request));

public class TdeeTool(CalculatorService calculators) : CalculatorTool<TdeeRequest>(
    "calculate_tdee",
    "Estimate daily calorie needs (TDEE) with the Mifflin-St Jeor formula, plus a cutting and a lean-bulk target.",
    """
    {
      "type": "object",
      "properties": {
        "units": { "type": "string", "enum": ["metric", "imperial"] },
        "weight": { "type": "number", "description": "kg for metric, lb for imperial." },
        "height_cm": { "type": "number" },
        "height_feet": { "type": "number" },
        "height_inches": { "type": "number" },
        "sex": { "type": "string", "enum": ["male", "female"] },
        "age": { "type": "integer" },
        "activity": {
          "type": "string",
          "enum": ["sedentary", "light", "moderate", "heavy", "athlete"],
          "description": "sedentary: desk job, little exercise. light: 1-3 workouts a week. moderate: 3-5. heavy: 6-7. athlete: twice a day or a physical job."
        }
      },
      "required": ["units", "weight", "sex", "age", "activity"]
    }
    """,
    request => calculators.Tdee(request));

public class MacrosTool(CalculatorService calculators) : CalculatorTool<MacroRequest>(
    "calculate_macros",
    "Split a daily calorie target into grams of protein, carbs and fat.",
    """
    {
      "type": "object",
      "properties": {
        "calories": { "type": "integer", "description": "Daily calories, e.g. from calculate_tdee." },
        "split": { "type": "string", "enum": ["balanced", "high_protein", "low_carb", "keto"] }
      },
      "required": ["calories", "split"]
    }
    """,
    request => calculators.Macros(request));

public class BodyFatTool(CalculatorService calculators) : CalculatorTool<BodyFatRequest>(
    "calculate_body_fat",
    "Estimate body fat percentage with the US Navy tape method. Women also need a hip measurement.",
    """
    {
      "type": "object",
      "properties": {
        "units": { "type": "string", "enum": ["metric", "imperial"] },
        "sex": { "type": "string", "enum": ["male", "female"] },
        "height_cm": { "type": "number" },
        "height_feet": { "type": "number" },
        "height_inches": { "type": "number" },
        "neck": { "type": "number", "description": "cm for metric, inches for imperial." },
        "waist": { "type": "number", "description": "At the navel. cm for metric, inches for imperial." },
        "hip": { "type": "number", "description": "Women only. cm for metric, inches for imperial." }
      },
      "required": ["units", "sex", "neck", "waist"]
    }
    """,
    request => calculators.BodyFat(request));