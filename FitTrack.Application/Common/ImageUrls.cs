namespace FitTrack.Application.Common;

public static class ImageUrls
{
    public const string InvalidMessage = "The image URL must be a valid http or https address.";

    public static bool IsValid(string? url) =>
        string.IsNullOrWhiteSpace(url)
        || (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

    /// <summary>Trims the URL and turns a blank value into null, which means "no image".</summary>
    public static string? Normalize(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : url.Trim();
}
