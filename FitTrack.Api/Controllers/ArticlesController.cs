using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/articles")]
public class ArticlesController(ArticleService articles) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ArticleDto>> Index(
        [FromQuery] PageQuery paging, [FromQuery] string? search, [FromQuery] string? category) =>
        articles.GetPageAsync(search, category, paging.ToPageRequest());

    [HttpGet("{key}")]
    public Task<ArticleDto> Show(string key) => articles.GetAsync(key);
}
