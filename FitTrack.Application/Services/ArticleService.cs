namespace FitTrack.Application.Services;

public class ArticleService(IArticleRepository articles)
{
    public async Task<PagedResult<ArticleDto>> GetPageAsync(string? search, string? category, PageRequest paging)
    {
        var filter = new ArticleFilter(
            search,
            EnumValues.TryParse<ArticleCategory>(category, out var parsed) ? parsed : null);

        var page = await articles.GetPublishedPageAsync(filter, paging, defaultPerPage: 12);
        return page.Map(a => a.ToDto());
    }

    public async Task<ArticleDto> GetAsync(string key)
    {
        var article = await articles.FindPublishedAsync(key)
                      ?? throw new NotFoundException("Article not found");

        var related = await articles.GetRelatedPublishedAsync(article, count: 3);

        return article.ToDto() with
        {
            Body = Mappings.Blocks(article.Body),
            Related = related.Select(a => a.ToDto()).ToList(),
        };
    }
}
