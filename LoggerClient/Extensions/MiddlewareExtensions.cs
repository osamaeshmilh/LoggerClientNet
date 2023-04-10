using Microsoft.AspNetCore.Builder;
using LoggerClient.Middleware;

namespace LoggerClient.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpLoggingMiddleware>();
        }
    }
}