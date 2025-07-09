using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ConnectCore.Shared.Middleware;

namespace ConnectCore.Shared.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }

    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }

    public static IApplicationBuilder UseServiceRegistration(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ServiceRegistrationMiddleware>();
    }
}
