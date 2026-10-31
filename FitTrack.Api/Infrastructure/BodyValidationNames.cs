using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace FitTrack.Api.Infrastructure;

// Reports JSON body errors under their snake_case names. Query models have to keep their
// bound names: a renamed PerPage is validated as per_page, so the bound entry stays
// unvalidated and ModelState turns invalid with no errors to report.
public class BodyValidationNames(JsonNamingPolicy policy) : IDisplayMetadataProvider, IValidationMetadataProvider
{
    private readonly SystemTextJsonValidationMetadataProvider _inner = new(policy);

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (!IsQueryModel(context.Key.ContainerType)) _inner.CreateDisplayMetadata(context);
    }

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        if (!IsQueryModel(context.Key.ContainerType)) _inner.CreateValidationMetadata(context);
    }

    private static bool IsQueryModel(Type? container) => typeof(PageQuery).IsAssignableFrom(container);
}
