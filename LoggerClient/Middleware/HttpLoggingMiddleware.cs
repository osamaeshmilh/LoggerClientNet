using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using LoggerClient.Models;
using LoggerClient.Kafka;
using Newtonsoft.Json;

namespace LoggerClient.Middleware
{
    public class HttpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IKafkaProducer _kafkaProducer;

        public HttpLoggingMiddleware(RequestDelegate next, IKafkaProducer kafkaProducer)
        {
            _next = next;
            _kafkaProducer = kafkaProducer;
        }

        public async Task InvokeAsync(HttpContext context)
        {
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

            logEntry.Timestamp = DateTime.UtcNow;
            logEntry.HttpMethod = context.Request.Method;
            logEntry.RequestUrl = context.Request.Path;
            logEntry.RequestUrlParameters = context.Request.QueryString.ToString();
            logEntry.HttpStatusCode = context.Response.StatusCode;
            logEntry.RemoteIpAddress = context.Connection.RemoteIpAddress.ToString();
            logEntry.Duration = stopwatch.ElapsedMilliseconds;
            logEntry.RequestHeaders = JsonConvert.SerializeObject(context.Request.Headers);
            logEntry.ResponseHeaders = JsonConvert.SerializeObject(context.Response.Headers);
            logEntry.RequestCookies = JsonConvert.SerializeObject(context.Request.Cookies);
            logEntry.ResponseCookies = JsonConvert.SerializeObject(context.Response.Cookies);

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            logEntry.RequestBody = await new StreamReader(requestBodyStream).ReadToEndAsync();

            if (context.Response.ContentType != null && context.Response.ContentType.Contains("application/json"))
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                logEntry.ResponseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            }

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);

            await _kafkaProducer.ProduceAsync(logEntry);
        }
    }
}
