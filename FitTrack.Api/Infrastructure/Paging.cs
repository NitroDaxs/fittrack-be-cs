using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Infrastructure;

public class PageQuery
{
    public int Page { get; set; } = 1;
    public int? PerPage { get; set; }

    [FromQuery(Name = "per_page")]
    public int? PerPageSnake { get; set; }

    public int Size(int fallback) => Math.Clamp(PerPage ?? PerPageSnake ?? fallback, 1, 100);
}

public record PagedResult<T>(List<T> Data, int CurrentPage, int PerPage, int Total, int LastPage, int? From, int? To);

public static class PagingExtensions
{
    public static async Task<PagedResult<TOut>> PaginateAsync<TIn, TOut>(
        this IQueryable<TIn> query, PageQuery paging, int defaultPerPage, Func<TIn, TOut> map)
    {
        var perPage = paging.Size(defaultPerPage);
        var page = Math.Max(paging.Page, 1);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync();

        var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
        int? from = items.Count == 0 ? null : (page - 1) * perPage + 1;

        return new PagedResult<TOut>(items.Select(map).ToList(), page, perPage, total, lastPage, from, from + items.Count - 1);
    }
}