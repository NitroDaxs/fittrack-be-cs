using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/taxonomies")]
public class TaxonomiesController(TaxonomyService taxonomies) : ControllerBase
{
    [HttpGet]
    public Task<TaxonomiesDto> Index() => taxonomies.GetAsync();
}
