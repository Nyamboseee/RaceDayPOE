using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Services;

// Catches API problems anywhere in the app and shows a friendly page, never a raw error
public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ApiUnauthorizedException:
                // The API session ended, so end the MVC session too and ask for a fresh login
                await context.HttpContext.SignOutAsync();
                context.HttpContext.Session.Clear();
                context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
                context.ExceptionHandled = true;
                break;

            case ApiException api:
                var kind = api.Status switch
                {
                    System.Net.HttpStatusCode.Forbidden => "forbidden",
                    System.Net.HttpStatusCode.NotFound => "notfound",
                    _ => "error"
                };
                context.Result = new RedirectToActionResult("Problem", "Home", new { kind });
                context.ExceptionHandled = true;
                break;

            case HttpRequestException or TaskCanceledException:
                _logger.LogWarning(context.Exception, "The API could not be reached.");
                context.Result = new RedirectToActionResult("Problem", "Home", new { kind = "api-down" });
                context.ExceptionHandled = true;
                break;
        }
    }
}
