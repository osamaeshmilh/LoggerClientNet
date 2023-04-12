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
        var applicationId = await _applicationAuthenticator.GetApplicationIdByTokenAsync(_configuration.AppToken);

        var logEntry = new LogEntry();

        var requestBodyStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(requestBodyStream);
        requestBodyStream.Seek(0, SeekOrigin.Begin);

        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        logEntry.request_timestamp = DateTime.UtcNow;
        logEntry.http_method = context.Request.Method;
        logEntry.request_url = context.Request.Path;
        logEntry.request_url_parameters = context.Request.QueryString.ToString();
        logEntry.http_status_code = context.Response.StatusCode.ToString();
        logEntry.remote_ip_address = context.Connection.RemoteIpAddress.ToString();
        logEntry.duration = stopwatch.ElapsedMilliseconds;
        logEntry.request_headers = JsonConvert.SerializeObject(context.Request.Headers);
        logEntry.response_headers = JsonConvert.SerializeObject(context.Response.Headers);
        logEntry.request_cookies = JsonConvert.SerializeObject(context.Request.Cookies);
        logEntry.response_cookies = JsonConvert.SerializeObject(context.Response.Cookies);
        logEntry.application_id = applicationId;

        requestBodyStream.Seek(0, SeekOrigin.Begin);
        logEntry.request_body = await new StreamReader(requestBodyStream).ReadToEndAsync();

        if (context.Response.ContentType != null && context.Response.ContentType.Contains("application/json"))
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            logEntry.response_body = await new StreamReader(responseBodyStream).ReadToEndAsync();
        }

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalBodyStream);

        if (applicationId != null)
            await _kafkaProducer.ProduceAsync(logEntry);
        else
            Console.WriteLine("No Application Id");
    }
}