using System.ComponentModel.DataAnnotations;
using FitTrack.Application.Common;
using FitTrack.Application.Dtos;

namespace FitTrack.Tests;

/// <summary>The request rules for new passwords, which the frontend's schemas mirror.</summary>
public class PasswordRulesTests
{
    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData("Password1")]
    [InlineData("Abcdefg1")]
    [InlineData("LONGER-PASSWORD-2026")]
    public void Accepts_8_plus_characters_with_a_number_and_an_uppercase_letter(string password)
    {
        Assert.Empty(Validate(new RegisterRequest { Name = "Alex", Email = "alex@example.com", Password = password }));
    }

    [Theory]
    [InlineData("Passwo1")]          // too short
    [InlineData("password1")]        // no uppercase letter
    [InlineData("Password")]         // no number
    public void Rejects_passwords_that_break_a_rule(string password)
    {
        var errors = Validate(new RegisterRequest { Name = "Alex", Email = "alex@example.com", Password = password });

        var error = Assert.Single(errors);
        Assert.Equal(PasswordRules.Message, error.ErrorMessage);
    }

    [Fact]
    public void Every_new_password_request_uses_the_same_rule()
    {
        Assert.Contains(Validate(new ResetPasswordRequest { Token = "t", Password = "password1", PasswordConfirmation = "password1" }),
            e => e.ErrorMessage == PasswordRules.Message);
        Assert.Contains(Validate(new ChangePasswordRequest { CurrentPassword = "x", NewPassword = "password1", NewPasswordConfirmation = "password1" }),
            e => e.ErrorMessage == PasswordRules.Message);
        Assert.Contains(Validate(new AdminUserRequest { Password = "password1" }),
            e => e.ErrorMessage == PasswordRules.Message);
    }

    [Fact]
    public void Logging_in_does_not_check_the_rule_so_older_passwords_still_work()
    {
        Assert.Empty(Validate(new LoginRequest { Email = "alex@example.com", Password = "old" }));
    }
}
