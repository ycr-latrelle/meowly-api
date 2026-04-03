using System.Net;
using Newtonsoft.Json;

namespace MeowlyAPI.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";
            var payload = JsonConvert.SerializeObject(new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message   // remove in production
            });
            await ctx.Response.WriteAsync(payload);
        }
    }
}
