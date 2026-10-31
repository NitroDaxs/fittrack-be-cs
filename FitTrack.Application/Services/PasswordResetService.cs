using System.Security.Cryptography;
using System.Text;
using FitTrack.Application.Abstractions.Email;

namespace FitTrack.Application.Services;

public class PasswordResetService(
    IUserRepository users,
    IPasswordResetTokenRepository tokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    IAccountEmailSender emails)
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(60);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Finishes silently when no email goes out, so callers cannot use the
    /// outcome to find out which addresses have accounts.
    /// </summary>
    public async Task RequestAsync(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (await tokens.WasIssuedSinceAsync(user.Id, now - ResendCooldown))
        {
            return;
        }

        await tokens.DeleteForUserAsync(user.Id);

        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        tokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAt = now + TokenLifetime,
        });
        await unitOfWork.SaveChangesAsync();

        await emails.SendPasswordResetAsync(user, token, TokenLifetime);
    }

    public async Task ResetAsync(ResetPasswordRequest request)
    {
        var token = await tokens.FindByHashAsync(Hash(request.Token));
        if (token is null || token.ExpiresAt <= DateTime.UtcNow)
        {
            throw InvalidToken();
        }

        var user = await users.FindAsync(token.UserId);
        if (user is null || !user.IsActive)
        {
            throw InvalidToken();
        }

        // Whoever deletes the token owns it: of two simultaneous requests, only one gets a row back.
        if (await tokens.DeleteForUserAsync(user.Id) == 0)
        {
            throw InvalidToken();
        }

        user.PasswordHash = hasher.Hash(request.Password);
        await unitOfWork.SaveChangesAsync();
    }

    private static ValidationFailedException InvalidToken() =>
        new("token", "This password reset link is invalid or has expired.");

    private static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
