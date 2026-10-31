using System.ComponentModel.DataAnnotations;

namespace FitTrack.Application.Dtos;

public record UserDto(
    int Id,
    string Name,
    string Email,
    UserRole Role,
    Units Units,
    bool IsActive,
    DateTime CreatedAt);

public record AuthResponse(
    UserDto User,
    string AccessToken,
    string TokenType = "Bearer");

public record RegisterRequest
{
    [Required, MaxLength(255)] public string Name { get; init; } = "";
    [Required, EmailAddress, MaxLength(255)] public string Email { get; init; } = "";
    [Required, MinLength(8)] public string Password { get; init; } = "";
}

public record ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
}

public record ResetPasswordRequest
{
    [Required] public string Token { get; init; } = "";
    [Required, MinLength(8)] public string Password { get; init; } = "";
    [Required, Compare(nameof(Password))] public string PasswordConfirmation { get; init; } = "";
}

public record LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required] public string Password { get; init; } = "";
}

public record UpdateProfileRequest
{
    [MaxLength(255)] public string? Name { get; init; }
    [EmailAddress] public string? Email { get; init; }
    public Units? Units { get; init; }

    /// <summary>Required when changing the email.</summary>
    public string? CurrentPassword { get; init; }
}

public record ChangePasswordRequest
{
    [Required(ErrorMessage = "Enter your current password.")]
    public string CurrentPassword { get; init; } = "";

    [Required(ErrorMessage = "Choose a new password.")]
    [RegularExpression(@"^(?=.*\d).{8,}$", ErrorMessage = "Use at least 8 characters, including a number.")]
    public string NewPassword { get; init; } = "";

    [Required(ErrorMessage = "Confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    public string NewPasswordConfirmation { get; init; } = "";
}
