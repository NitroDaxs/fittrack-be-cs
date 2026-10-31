namespace FitTrack.Application.Abstractions.Persistence;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindByHashAsync(string tokenHash);
    Task<bool> WasIssuedSinceAsync(int userId, DateTime since);
    void Add(PasswordResetToken token);

    /// <returns>The number of tokens deleted.</returns>
    Task<int> DeleteForUserAsync(int userId);
}
