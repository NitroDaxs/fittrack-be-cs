namespace FitTrack.Application.Abstractions.Email;

public interface IAccountEmailSender
{
    Task SendPasswordResetAsync(User user, string token, TimeSpan validFor);
}
