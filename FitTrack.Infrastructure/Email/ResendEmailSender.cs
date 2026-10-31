using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FitTrack.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitTrack.Infrastructure.Email;

// A failed send is logged rather than thrown: the forgot-password response must
// look the same whether or not an email went out.
public class ResendEmailSender(HttpClient http, IOptions<EmailOptions> options, ILogger<ResendEmailSender> logger)
    : IAccountEmailSender
{
    public async Task SendPasswordResetAsync(User user, string token, TimeSpan validFor)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ResendApiKey))
        {
            logger.LogError("Email:ResendApiKey is not set, so the password reset email was not sent.");
            return;
        }

        var link = $"{settings.FrontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
        var name = WebUtility.HtmlEncode(user.Name);
        var minutes = (int)validFor.TotalMinutes;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new
            {
                from = settings.From,
                to = new[] { user.Email },
                subject = "Reset your FitTrack password",
                html = $"""
                    <p>Hi {name},</p>
                    <p>We received a request to reset your FitTrack password.</p>
                    <p><a href="{WebUtility.HtmlEncode(link)}">Choose a new password</a></p>
                    <p>This link works once and expires in {minutes} minutes. If you didn't ask for this, you can ignore this email.</p>
                    """,
                text = $"""
                    Hi {user.Name},

                    We received a request to reset your FitTrack password. Choose a new one here:
                    {link}

                    This link works once and expires in {minutes} minutes. If you didn't ask for this, you can ignore this email.
                    """,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ResendApiKey);

        try
        {
            using var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Resend rejected the password reset email for user {UserId}: {Status} {Body}",
                    user.Id, (int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(exception, "Could not reach Resend to email user {UserId}.", user.Id);
        }
    }
}
