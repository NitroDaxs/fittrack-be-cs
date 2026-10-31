using FitTrack.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api/taxonomies")]
public class TaxonomiesController(TaxonomyService taxonomies) : ControllerBase
{
    [HttpGet]
    public Task<TaxonomiesDto> Index() => taxonomies.GetAsync();
}
