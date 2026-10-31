using System.Text.Json;
using System.Text.RegularExpressions;

namespace FitTrack.Application.Services.Admin;

public class AdminArticleService(IArticleRepository articles, IUnitOfWork unitOfWork)
{
    private record Block(string Type, string Text);

    private static readonly JsonSerializerOptions BlockJson = new(JsonSerializerDefaults.Web);

    public async Task<PagedResult<ArticleDto>> GetPageAsync(AdminListRequest query, PageRequest paging)
    {
        var filter = new ArticleFilter(
            query.Search,
            EnumValues.TryParse<ArticleCategory>(query.Category, out var category) ? category : null,
            EnumValues.TryParse<ArticleStatus>(query.Status, out var status) ? status : null,
            string.IsNullOrWhiteSpace(query.Author) ? null : query.Author);

        var page = await articles.GetPageAsync(filter, new SortRequest(query.Sort, query.Dir), paging, defaultPerPage: 20);
        return page.Map(a => a.ToDto());
    }

    public async Task<ArticleDto> CreateAsync(int userId, ArticleRequest request)
    {
        ValidationFailedException.ThrowIfAny(
            (string.IsNullOrWhiteSpace(request.Title), "title", "The title field is required."),
            (request.Category is null, "category", "The category field is required."));

        var status = request.Status ?? ArticleStatus.Draft;
        var article = new Article
        {
            Title = request.Title!,
            Slug = Slug.From(request.Title!),
            Body = ToBlocksJson(request.Body, request.Excerpt),
            Category = request.Category!.Value,
            Status = status,
            PublishedAt = status == ArticleStatus.Published ? DateTime.UtcNow : null,
            AuthorId = userId,
        };

        if (await articles.SlugTakenAsync(article.Slug))
        {
            throw new ValidationFailedException("title", "An article with this title already exists.");
        }

        articles.Add(article);
        await unitOfWork.SaveChangesAsync();

        return (await articles.GetWithAuthorAsync(article.Id)).ToDto();
    }

    public async Task<ArticleDto> UpdateAsync(int id, ArticleRequest request)
    {
        var article = await articles.FindAsync(id)
                      ?? throw new NotFoundException("Article not found.");

        if (request.Title is not null)
        {
            var slug = Slug.From(request.Title);
            if (await articles.SlugTakenAsync(slug, exceptId: id))
            {
                throw new ValidationFailedException("title", "An article with this title already exists.");
            }

            article.Title = request.Title;
            article.Slug = slug;
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

        await unitOfWork.SaveChangesAsync();
        return (await articles.GetWithAuthorAsync(id)).ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        if (await articles.DeleteAsync(id) == 0)
        {
            throw new NotFoundException("Article not found.");
        }
    }

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
}
