using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FitTrack.Api.Http;

// Turns the exceptions application services throw into the API's error responses.
public class ApplicationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        IActionResult? result = context.Exception switch
        {
            NotFoundException e => new NotFoundObjectResult(new { message = e.Message }),
            RuleViolationException e => ApiErrors.Message(e.Message),
            ValidationFailedException e => ApiErrors.Validation(ToModelState(e)),
            _ => null,
        };

        if (result is not null)
        {
            context.Result = result;
            context.ExceptionHandled = true;
        }
    }

    private static ModelStateDictionary ToModelState(ValidationFailedException exception)
    {
        var modelState = new ModelStateDictionary();
        foreach (var (field, message) in exception.Errors)
        {
            modelState.AddModelError(field, message);
        }

        return modelState;
    }
}
