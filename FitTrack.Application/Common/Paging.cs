namespace FitTrack.Application.Common;

public record PageRequest(int Page = 1, int? PerPage = null)
{
    public int Number => Math.Max(Page, 1);

    public int Size(int fallback) => Math.Clamp(PerPage ?? fallback, 1, 100);
}

public record SortRequest(string? Column, string? Direction);

public record PagedResult<T>(List<T> Data, int CurrentPage, int PerPage, int Total, int LastPage, int? From, int? To)
{
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) =>
        new(Data.Select(map).ToList(), CurrentPage, PerPage, Total, LastPage, From, To);
}

public static class PagedResult
{
    public static PagedResult<T> Create<T>(List<T> items, int page, int perPage, int total)
    {
        var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
        int? from = items.Count == 0 ? null : (page - 1) * perPage + 1;

        return new PagedResult<T>(items, page, perPage, total, lastPage, from, from + items.Count - 1);
    }
}
