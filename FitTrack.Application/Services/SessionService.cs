using System.Security.Cryptography;
using System.Text;

namespace FitTrack.Application.Services;

/// <summary>
/// Checks a token against the current account on every request, so suspending or
/// deleting a user, changing their role or changing their password takes effect
/// straight away rather than when the token expires.
/// </summary>
public class SessionService(IUserRepository users)
{
    /// <summary>
    /// Goes in the token and changes whenever the password does, which ends the
    /// sessions issued before the change. It reveals nothing usable about the password.
    /// </summary>
    public static string StampFor(User user) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(user.PasswordHash)))[..16];

    /// <summary>Returns the account, or null if the token should no longer be accepted.</summary>
    public async Task<User?> FindValidUserAsync(int userId, string? stamp)
    {
        var user = await users.FindAsync(userId);
        return user is { IsActive: true } && stamp == StampFor(user) ? user : null;
    }
}
