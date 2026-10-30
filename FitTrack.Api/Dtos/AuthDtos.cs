using System.ComponentModel.DataAnnotations;

namespace FitTrack.Api.Dtos;

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
    public string? CurrentPassword { get; init; }
    [MinLength(8)] public string? NewPassword { get; init; }
    [Compare(nameof(NewPassword))] public string? NewPasswordConfirmation  { get; init; }
}
    