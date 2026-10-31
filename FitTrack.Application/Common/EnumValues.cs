using System.Text.Json;
using System.Text.RegularExpressions;

namespace FitTrack.Application.Common;

public static class EnumValues
{
    public static string ToValue(this Enum value) =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    public static string ToLabel(this Enum value) =>
        Regex.Replace(value.ToString(), "(?<!^)([A-Z])", " $1");
    
    public static TEnum Parse<TEnum>(string text) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(Normalize(text), ignoreCase: true);

    public static bool TryParse<TEnum>(string? text, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        return !string.IsNullOrWhiteSpace(text)
            && Enum.TryParse(Normalize(text), ignoreCase:true, out value)
            && Enum.IsDefined(value);
    }

    public static List<OptionDto> Options<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Select(v => new OptionDto(v.ToValue(), v.ToLabel(), v.ToValue()))
            .ToList();
    
    private static string Normalize(string text) =>
        text.Replace("_", "").Replace("-", "").Replace(" ", "");
}

public record OptionDto(string Id, string Name, string Slug);

