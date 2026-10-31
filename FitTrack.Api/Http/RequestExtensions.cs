using System.Security.Claims;

namespace FitTrack.Api.Http;

public static class RequestExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue("sub") ?? throw new InvalidOperationException("The token has no user id."));

    public static List<string> QueryList(this HttpRequest request, params string[] names) =>
        names
            .SelectMany(name => request.Query[name])
            .SelectMany(value => (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();
}
