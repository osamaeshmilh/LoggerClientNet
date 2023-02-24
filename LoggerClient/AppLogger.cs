using Confluent.Kafka;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace LoggerClient
{
    //key oy2gh66m7e3jz4b6yxsafb4y2gljvqyenaidge6w4t6nmi


    public class AppLogger
    {
        public async Task LogAsync(string text)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "host1:9092",
                
            };

            using (var producer = new ProducerBuilder<Null, string>(config).Build())
            {
                var result = await producer.ProduceAsync("weblog", new Message<Null, string> { Value = "a log message" });
            }
            Console.WriteLine(text);
        }
    }

    public class HttpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpLoggingMiddleware> _logger;

        public HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
}

        public async Task Invoke(HttpContext context)
        {
            // Log the request information
            var request = context.Request;
            var requestBody = string.Empty;

            //using (var reader = new StreamReader(request.Body))
            //{
            //    requestBody = await reader.ReadToEndAsync();
            //}

            _logger.LogInformation("HTTP Request {Method} {Uri} {Content}", request.Method, request.Path, request.Body);

            // Call the next middleware in the pipeline
            await _next(context);

            // Log the response information
            var response = context.Response.Body;

            if (response != null )
            {
                _logger.LogInformation("HTTP Response {StatusCode} {Uri} {Content}", context.Response.StatusCode, request.Path, response);
            }
        }
    }
}