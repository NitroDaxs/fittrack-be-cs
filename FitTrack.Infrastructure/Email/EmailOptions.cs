namespace FitTrack.Infrastructure.Email;

public class EmailOptions
{
    public string ResendApiKey { get; set; } = "";
    public string From { get; set; } = "";
    public string FrontendUrl { get; set; } = "";
}
