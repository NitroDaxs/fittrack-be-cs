namespace FitTrack.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository(AppDbContext db) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> FindByHashAsync(string tokenHash) =>
        db.PasswordResetTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public Task<bool> WasIssuedSinceAsync(int userId, DateTime since) =>
        db.PasswordResetTokens.AnyAsync(t => t.UserId == userId && t.CreatedAt >= since);

    public void Add(PasswordResetToken token) => db.PasswordResetTokens.Add(token);

    public Task<int> DeleteForUserAsync(int userId) =>
        db.PasswordResetTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync();
}
