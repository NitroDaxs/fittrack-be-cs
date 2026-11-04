namespace FitTrack.Application.Common;

/// <summary>
/// The rule for every new password, matching the frontend's form schemas:
/// at least 8 characters, including a number and an uppercase letter.
/// Logging in does not check it, so older passwords keep working.
/// </summary>
public static class PasswordRules
{
    public const string Pattern = @"^(?=.*\d)(?=.*[A-Z]).{8,}$";

    public const string Message = "Use at least 8 characters, including a number and an uppercase letter.";
}
