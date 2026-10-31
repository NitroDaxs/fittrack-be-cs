using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/articles")]
public class AdminArticlesController(AppDbContext db) : ControllerBase
{
    private static readonly Dictionary<string, Expression<Func<Article, object>>> Sortable = new()
    {
        ["title"] = a => a.Title,
        ["category"] = a => a.Category,
        ["status"] = a => a.Status,
        ["published_at"] = a => a.PublishedAt!,
        ["created_at"] = a => a.CreatedAt,
    };
    
    [HttpGet]
    public async Task<PagedResult<ArticleDto>> Index([FromQuery] AdminListQuery query)
    {
        IQueryable<Article> articles = db.Articles.AsNoTracking().Include(a => a.Author);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            articles = articles.Where(a => EF.Functions.ILike(a.Title, pattern) || EF.Functions.ILike(a.Body, pattern));
        }
        if (EnumValues.TryParse<ArticleStatus>(query.Status, out var status))
        {
            articles = articles.Where(a => a.Status == status);
        }

        return await articles
            .SortBy(query.Sort, query.Dir, Sortable, fallback: "created_at")
            .PaginateAsync(query, defaultPerPage: 20, a => a.ToDto());
    }
    
    [HttpPost]
    public async Task<IActionResult> Store(ArticleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) ModelState.AddModelError("title", "The title field is required.");
        if (request.Category is null) ModelState.AddModelError("category", "The category field is required.");
        if (!ModelState.IsValid) return ApiErrors.Validation(ModelState);

        var status = request.Status ?? ArticleStatus.Draft;
        var article = new Article
        {
            Title = request.Title!,
            Slug = Slug.From(request.Title!),
            Body = ToBlocksJson(request.Body, request.Excerpt),
            Category = request.Category!.Value,
            Status = status,
            PublishedAt = status == ArticleStatus.Published ? DateTime.UtcNow : null,
            AuthorId = User.GetUserId(),
        };

        if (await db.Articles.AnyAsync(a => a.Slug == article.Slug))
        {
            return ApiErrors.Field("title", "An article with this title already exists.");
        }

        db.Articles.Add(article);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await LoadDtoAsync(article.Id));
    }
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ArticleRequest request)
    {
        var article = await db.Articles.FindAsync(id);
        if (article is null)
        {
            return NotFound(new { message = "Article not found." });
        }

        if (request.Title is not null)
        {
            article.Title = request.Title;
            article.Slug = Slug.From(request.Title);
        }
        article.Category = request.Category ?? article.Category;

        if (request.Body is { ValueKind: not JsonValueKind.Null } || request.Excerpt is not null)
        {
            article.Body = ToBlocksJson(request.Body, request.Excerpt);
        }

        if (request.Status == ArticleStatus.Published && article.Status != ArticleStatus.Published)
        {
            article.PublishedAt = DateTime.UtcNow;
        }
        else if (request.Status == ArticleStatus.Draft)
        {
            article.PublishedAt = null;
        }
        article.Status = request.Status ?? article.Status;

        await db.SaveChangesAsync();
        return Ok(await LoadDtoAsync(id));
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Destroy(int id)
    {
        var deleted = await db.Articles.Where(a => a.Id == id).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound(new { message = "Article not found." }) : Ok(new { status = "deleted" });
    }

    private record Block(string Type, string Text);

    private static readonly JsonSerializerOptions BlockJson = new(JsonSerializerDefaults.Web);
    
    private static string ToBlocksJson(JsonElement? body, string? excerpt)
    {
        if (body is { ValueKind: JsonValueKind.Array } blocks && blocks.GetArrayLength() > 0)
        {
            return blocks.GetRawText();
        }

        var text = body is { ValueKind: JsonValueKind.String } s && !string.IsNullOrWhiteSpace(s.GetString())
            ? s.GetString()!
            : excerpt ?? "";

        var parsed = Regex.Split(text.Trim(), @"\n\s*\n")
            .Select(chunk => chunk.Trim())
            .Where(chunk => chunk.Length > 0)
            .Select(chunk => chunk.EndsWith(':') && chunk.Length < 80
                ? new Block("h2", chunk.TrimEnd(':'))
                : new Block("p", chunk))
            .ToList();

        return JsonSerializer.Serialize(parsed.Count > 0 ? parsed : [new Block("p", "")], BlockJson);
    }

    private async Task<ArticleDto> LoadDtoAsync(int id) =>
        (await db.Articles.AsNoTracking().Include(a => a.Author).FirstAsync(a => a.Id == id)).ToDto();
}