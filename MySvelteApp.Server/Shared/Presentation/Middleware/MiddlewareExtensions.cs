using MySvelteApp.Server.Shared.Presentation.Middleware;

namespace MySvelteApp.Server.Shared.Presentation.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

