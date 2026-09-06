using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RedAnts.Domain;

namespace RedAnts.Infrastructure.Shared;

public sealed class DomainErrorFilter(ITempDataDictionaryFactory tempDataFactory) : IExceptionFilter
{
    public const string TempDataKey = "DomainError";

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ValidationException validation:
                context.Result = WantsJson(context.HttpContext)
                    ? ValidationProblem(validation)
                    : RedirectBack(context.HttpContext, validation.Message);
                break;
            case DomainException domain:
                context.Result = WantsJson(context.HttpContext)
                    ? Problem(domain.Message)
                    : RedirectBack(context.HttpContext, domain.Message);
                break;
            default:
                return;
        }
        context.ExceptionHandled = true;
    }

    private static bool WantsJson(HttpContext http)
    {
        var path = http.Request.Path;
        if (path.StartsWithSegments("/api") || path.StartsWithSegments("/payrexx")) return true;
        var accept = http.Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static IActionResult ValidationProblem(ValidationException validation)
    {
        var details = new ValidationProblemDetails { Status = StatusCodes.Status400BadRequest, Title = validation.Message };
        details.Errors[validation.Field] = [validation.Message];
        return new BadRequestObjectResult(details);
    }

    private static IActionResult Problem(string message) =>
        new BadRequestObjectResult(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = message });

    private IActionResult RedirectBack(HttpContext http, string message)
    {
        tempDataFactory.GetTempData(http)[TempDataKey] = message;
        var referer = http.Request.Headers.Referer.ToString();
        var target = Uri.TryCreate(referer, UriKind.Absolute, out var uri) && uri.Host == http.Request.Host.Host
            ? uri.PathAndQuery
            : http.Request.Path + http.Request.QueryString;
        return new RedirectResult(target);
    }
}
