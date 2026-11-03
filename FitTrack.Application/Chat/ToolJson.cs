using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FitTrack.Application.Chat;

public static class ToolJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
    
    public static JsonElement Schema(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);

    public static string Error(string message) => Serialize(new { Error = message });
}

public readonly struct ToolArguments(JsonElement json)
{
    public JsonElement Json => json;

    public string? String(string name) =>
        TryGet(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;

    public int? Int(string name)
    {
        if (!TryGet(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)) return (int)Math.Round(number);
        return value.ValueKind == JsonValueKind.String
               && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
    
    public List<string> Strings(string name)
    {
        if (!TryGet(name, out var value)) return [];
        if (value.ValueKind == JsonValueKind.String) return String(name) is { } single ? [single] : [];
        if (value.ValueKind != JsonValueKind.Array) return [];

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!.Trim())
            .ToList();
    }

    public List<ToolArguments> Objects(string name) =>
        TryGet(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object).Select(item => new ToolArguments(item)).ToList()
            : [];

    public string Required(string name) =>
        String(name) ?? throw new ToolArgumentException($"The \"{name}\" argument is required.");

    private bool TryGet(string name, out JsonElement value)
    {
        value = default;
        return json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null;
    }
}