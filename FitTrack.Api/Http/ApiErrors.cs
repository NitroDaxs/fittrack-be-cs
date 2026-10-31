using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FitTrack.Api.Http;

public static class ApiErrors
{
    public static UnprocessableEntityObjectResult Validation(ModelStateDictionary modelState)
    {
        var jsonErrorsOnly = modelState.Keys.Any(key => key.StartsWith('$'));

        var errors = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 } && (!jsonErrorsOnly || entry.Key.StartsWith('$')))
            .ToDictionary(
                entry => entry.Key.TrimStart('$', '.'),
                entry => entry.Value!.Errors.Select(error => Describe(entry.Key, error)).ToArray());

        return Body(errors);
    }

    public static UnprocessableEntityObjectResult Field(string field, string message) =>
        Body(new Dictionary<string, string[]> { [field] = [message] });

    public static UnprocessableEntityObjectResult Message(string message) =>
        new(new { message });

    private static UnprocessableEntityObjectResult Body(Dictionary<string, string[]> errors) =>
        new(new
        {
            message = errors.Values.FirstOrDefault()?.FirstOrDefault() ?? "The given data was invalid.",
            errors,
        });
    
    private static string Describe(string key, ModelError error)
    {
        if (!key.StartsWith('$'))
        {
            return error.ErrorMessage;
        }

        var field = key.TrimStart('$', '.');
        return field.Length == 0 ? "The request body is not valid JSON." : $"The {field} field has an invalid value.";
    }
}