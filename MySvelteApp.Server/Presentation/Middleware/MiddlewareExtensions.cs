using MySvelteApp.Server.Presentation.Middleware;

namespace MySvelteApp.Server.Presentation.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

