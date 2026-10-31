using System.Text.RegularExpressions;

namespace FitTrack.Application.Common;

public static partial class Slug
{
    public static string From(string text)
    {
        var slug = text.ToLowerInvariant().Replace("_", "-").Replace("@", "-at-");
        slug = Invalid().Replace(slug, "");
        slug = Separators().Replace(slug, "-");
        return slug.Trim('-');
    }
    
    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex Invalid();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex Separators();
}

