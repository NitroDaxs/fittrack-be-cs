using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<ArticleDto>> Index([FromQuery] PageQuery paging, [FromQuery] string? search,
        [FromQuery] string? category)
    {
        var articles = db.Articles.AsNoTracking()
            .Include(a => a.Author)
            .Where(a => a.Status == ArticleStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            articles = articles.Where(a => EF.Functions.ILike(a.Title, pattern) || EF.Functions.ILike(a.Body, pattern));
        }

        if (EnumValues.TryParse<ArticleCategory>(category, out var parsedCategory))
        {
            articles = articles.Where(a => a.Category == parsedCategory);
        }

        return await articles
            .OrderByDescending(a => a.PublishedAt)
            .PaginateAsync(paging, defaultPerPage: 12, a => a.ToDto());
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<ArticleDto?>> Show(string key)
    {
        var id = int.TryParse(key, out var parsed) ? parsed : 0;

        var article = await db.Articles.AsNoTracking().Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Status == ArticleStatus.Published && (a.Id == id || a.Slug == key));

        if (article is null)
        {
            return NotFound(new { message = "Article not found" });
        }

        var related = await db.Articles.AsNoTracking().Include(a => a.Author)
            .Where(a => a.Status == ArticleStatus.Published && a.Id != article.Id)
            .OrderByDescending(a => a.Category == article.Category)
            .ThenByDescending(a => a.PublishedAt)
            .Take(3)
            .ToListAsync();

        return article.ToDto() with
        {
            Body = Mappings.Blocks(article.Body),
            Related = related.Select(a => a.ToDto()).ToList(),
        };
    }
}