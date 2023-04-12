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
            //TODO:: application token
            logEntry.application_id = 1;

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            logEntry.request_body = await new StreamReader(requestBodyStream).ReadToEndAsync();

            if (context.Response.ContentType != null && context.Response.ContentType.Contains("application/json"))
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                logEntry.response_body = await new StreamReader(responseBodyStream).ReadToEndAsync();
            }

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);

            await _kafkaProducer.ProduceAsync(logEntry);
        }
    }
}
