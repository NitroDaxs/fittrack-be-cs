using FitTrack.Application.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FitTrack.Api.Http;

/// <summary>
/// Rejects an oversized avatar before the form is read, so the client gets the same
/// message as a file that only just misses the limit, not the framework's error.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RejectOversizedAvatarAttribute : Attribute, IResourceFilter
{
    // Room for the multipart boundaries and headers around the file.
    public const long MaxRequestBytes = AvatarService.MaxBytes + 64 * 1024;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (context.HttpContext.Request.ContentLength > MaxRequestBytes)
        {
            context.Result = ApiErrors.Field("file", AvatarService.TooLargeMessage);
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
