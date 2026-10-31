using FitTrack.Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/articles")]
public class AdminArticlesController(AdminArticleService articles) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ArticleDto>> Index([FromQuery] AdminListQuery query) =>
        articles.GetPageAsync(query.ToRequest(), query.ToPageRequest());

    [HttpPost]
    public async Task<IActionResult> Store(ArticleRequest request) =>
        StatusCode(StatusCodes.Status201Created, await articles.CreateAsync(User.GetUserId(), request));

    [HttpPut("{id:int}")]
    public Task<ArticleDto> Update(int id, ArticleRequest request) => articles.UpdateAsync(id, request);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Destroy(int id)
    {
        await articles.DeleteAsync(id);
        return Ok(new { status = "deleted" });
    }
}
