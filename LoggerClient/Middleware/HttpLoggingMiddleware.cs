using System.Diagnostics;
using LoggerClient.Authentication;
using LoggerClient.Configurations;
using LoggerClient.Kafka;
using LoggerClient.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace LoggerClient.Middleware;

public class HttpLoggingMiddleware
{
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "Set-Cookie", "X-Api-Key"
    };

    private readonly ApplicationAuthenticator _applicationAuthenticator;
    private readonly LoggerClientConfiguration _configuration;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly RequestDelegate _next;

    public HttpLoggingMiddleware(
        RequestDelegate next,
        IKafkaProducer kafkaProducer,
        ApplicationAuthenticator applicationAuthenticator,
        LoggerClientConfiguration configuration
    )
    {
        _next = next;
        _kafkaProducer = kafkaProducer;
        _applicationAuthenticator = applicationAuthenticator;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var applicationId = await _applicationAuthenticator.GetApplicationIdAsync();

        // Logging must never break the host app: pass the request through without logging.
        if (applicationId == null)
        {
            await _next(context);
            return;
        }

        var logEntry = new LogEntry
        {
            request_timestamp = DateTime.UtcNow,
            application_id = applicationId
        };

        // Buffer the request body so the rest of the pipeline can still read it.
        context.Request.EnableBuffering();
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            logEntry.request_body = await reader.ReadToEndAsync();
        }
        context.Request.Body.Position = 0;

        // Buffer the response so it can be inspected, then sent to the client.
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            try
            {
                logEntry.http_method = context.Request.Method;
                logEntry.request_url = context.Request.Path;
                logEntry.request_url_parameters = context.Request.QueryString.ToString();
                logEntry.http_status_code = context.Response.StatusCode.ToString();
                logEntry.remote_ip_address = context.Connection.RemoteIpAddress?.ToString();
                logEntry.duration = stopwatch.ElapsedMilliseconds;
                logEntry.request_headers = JsonConvert.SerializeObject(ToSafeHeaders(context.Request.Headers));
                logEntry.response_headers = JsonConvert.SerializeObject(ToSafeHeaders(context.Response.Headers));
                logEntry.request_cookies = JsonConvert.SerializeObject(
                    context.Request.Cookies.Keys.ToDictionary(k => k, _ => "[Redacted]"));

                if (context.Response.ContentType?.Contains("application/json") == true)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    using var responseReader = new StreamReader(responseBodyStream, leaveOpen: true);
                    logEntry.response_body = await responseReader.ReadToEndAsync();
                }

                await _kafkaProducer.ProduceAsync(logEntry);
            }
            catch (Exception ex)
            {
                // A logging failure must not affect the response.
                Console.WriteLine($"LoggerClient failed to publish log entry: {ex.Message}");
            }
            finally
            {
                // Always send the buffered response to the client, even if the pipeline threw.
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }
    }

    private static Dictionary<string, string> ToSafeHeaders(IHeaderDictionary headers) =>
        headers.ToDictionary(
            h => h.Key,
            h => SensitiveHeaders.Contains(h.Key) ? "[Redacted]" : h.Value.ToString());
}