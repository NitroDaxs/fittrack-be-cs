using System.Linq.Expressions;
using System.Security.Claims;

namespace FitTrack.Api.Infrastructure;

public static class RequestExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue("sub") ?? throw new InvalidOperationException("The token has no user id."));
    
    public static List<string> QueryList(this HttpRequest request, params string[] names) =>
        names
            .SelectMany(name => request.Query[name])
            .SelectMany(value => (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();

    public static IQueryable<T> SortBy<T>(
        this IQueryable<T> query, string? column, string? dir, Dictionary<string, Expression<Func<T, object>>> columns,
        string fallback)
    {
        var key = column is not null && columns.ContainsKey(column) ? column : fallback;
        return dir == "asc" ? query.OrderBy(columns[key]) : query.OrderByDescending(columns[key]);
    }
}