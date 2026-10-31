namespace FitTrack.Application.Abstractions.Persistence;

public record ArticleFilter(
    string? Search,
    ArticleCategory? Category,
    ArticleStatus? Status = null,
    string? Author = null);

public interface IArticleRepository
{
    Task<PagedResult<Article>> GetPublishedPageAsync(ArticleFilter filter, PageRequest paging, int defaultPerPage);
    Task<PagedResult<Article>> GetPageAsync(ArticleFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage);

    /// <param name="key">An id or a slug.</param>
    Task<Article?> FindPublishedAsync(string key);

    Task<List<Article>> GetRelatedPublishedAsync(Article article, int count);

    /// <summary>Read-only with its author; throws if it does not exist.</summary>
    Task<Article> GetWithAuthorAsync(int id);

    /// <summary>Change-tracked.</summary>
    Task<Article?> FindAsync(int id);

    Task<bool> SlugTakenAsync(string slug, int? exceptId = null);
    void Add(Article article);

    /// <returns>The number of rows deleted.</returns>
    Task<int> DeleteAsync(int id);
}
