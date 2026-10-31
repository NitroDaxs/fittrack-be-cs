namespace FitTrack.Application.Common;

// Thrown by application services and translated to HTTP responses by the Api layer.

public class NotFoundException(string message) : Exception(message);

/// <summary>A request the service refuses, with no particular field to blame.</summary>
public class RuleViolationException(string message) : Exception(message);

public class ValidationFailedException(IReadOnlyDictionary<string, string> errors)
    : Exception(errors.Values.FirstOrDefault() ?? "The given data was invalid.")
{
    public IReadOnlyDictionary<string, string> Errors { get; } = errors;

    public ValidationFailedException(string field, string message)
        : this(new Dictionary<string, string> { [field] = message })
    {
    }

    public static void ThrowIfAny(params (bool Failed, string Field, string Message)[] checks)
    {
        var errors = checks.Where(c => c.Failed).ToDictionary(c => c.Field, c => c.Message);
        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }
    }
}
