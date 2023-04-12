using LoggerClient.Middleware;
using Microsoft.AspNetCore.Builder;

namespace LoggerClient.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseHttpLoggingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpLoggingMiddleware>();
    }
}