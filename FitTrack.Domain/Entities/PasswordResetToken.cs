namespace FitTrack.Domain.Entities;

public class PasswordResetToken : Timestamped
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Only the hash is stored, so a database leak does not expose usable reset links.
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
