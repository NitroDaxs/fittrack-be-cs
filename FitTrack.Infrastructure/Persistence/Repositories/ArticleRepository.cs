using System.Linq.Expressions;

namespace FitTrack.Infrastructure.Persistence.Repositories;

public class ArticleRepository(AppDbContext db) : IArticleRepository
{
    private static readonly Dictionary<string, Expression<Func<Article, object>>> Sortable = new()
    {
        ["title"] = a => a.Title,
        ["category"] = a => a.Category,
        ["status"] = a => a.Status,
        ["published_at"] = a => a.PublishedAt!,
        ["created_at"] = a => a.CreatedAt,
        ["date"] = a => a.PublishedAt ?? a.CreatedAt,
        ["author"] = a => a.Author.Name,
    };

    public Task<PagedResult<Article>> GetPublishedPageAsync(ArticleFilter filter, PageRequest paging, int defaultPerPage) =>
        Apply(WithAuthor().Where(a => a.Status == ArticleStatus.Published), filter)
            .OrderByDescending(a => a.PublishedAt)
            .PaginateAsync(paging, defaultPerPage);

    public Task<PagedResult<Article>> GetPageAsync(ArticleFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage) =>
        Apply(WithAuthor(), filter)
            .SortBy(sort, Sortable, fallback: "created_at")
            .PaginateAsync(paging, defaultPerPage);

    public Task<Article?> FindPublishedAsync(string key)
    {
        var id = int.TryParse(key, out var parsed) ? parsed : 0;

        return WithAuthor()
            .FirstOrDefaultAsync(a => a.Status == ArticleStatus.Published && (a.Id == id || a.Slug == key));
    }

    public Task<List<Article>> GetRelatedPublishedAsync(Article article, int count) =>
        WithAuthor()
            .Where(a => a.Status == ArticleStatus.Published && a.Id != article.Id)
            .OrderByDescending(a => a.Category == article.Category)
            .ThenByDescending(a => a.PublishedAt)
            .Take(count)
            .ToListAsync();

    public Task<Article> GetWithAuthorAsync(int id) => WithAuthor().FirstAsync(a => a.Id == id);

    public async Task<Article?> FindAsync(int id) => await db.Articles.FindAsync(id);

    public Task<bool> SlugTakenAsync(string slug, int? exceptId = null) =>
        db.Articles.AnyAsync(a => a.Slug == slug && (exceptId == null || a.Id != exceptId));

    public void Add(Article article) => db.Articles.Add(article);

    public Task<int> DeleteAsync(int id) => db.Articles.Where(a => a.Id == id).ExecuteDeleteAsync();

    private IQueryable<Article> WithAuthor() => db.Articles.AsNoTracking().Include(a => a.Author);

    private static IQueryable<Article> Apply(IQueryable<Article> articles, ArticleFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = filter.Search.ToSearchPattern();
            articles = articles.Where(a => EF.Functions.ILike(a.Title, pattern) || EF.Functions.ILike(a.Body, pattern));
        }

        if (filter.Status is { } status)
        {
            articles = articles.Where(a => a.Status == status);
        }

        if (filter.Category is { } category)
        {
            articles = articles.Where(a => a.Category == category);
        }

        if (filter.Author is { } author)
        {
            articles = articles.Where(a => a.Author.Name == author);
        }

        return articles;
    }
}
