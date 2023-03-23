using Confluent.Kafka;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace LoggerClient
{
    //key oy2gh66m7e3jz4b6yxsafb4y2gljvqyenaidge6w4t6nmi

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

            var config = new ProducerConfig
            {
                BootstrapServers = "cluster.playground.cdkt.io:9092",
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "shu",
                SaslPassword = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczovL2F1dGguY29uZHVrdG9yLmlvIiwic291cmNlQXBwbGljYXRpb24iOiJhZG1pbiIsInVzZXJNYWlsIjpudWxsLCJwYXlsb2FkIjp7InZhbGlkRm9yVXNlcm5hbWUiOiJzaHUiLCJvcmdhbml6YXRpb25JZCI6NzE0MjgsInVzZXJJZCI6bnVsbCwiZm9yRXhwaXJhdGlvbkNoZWNrIjoiMTUzYzViNGQtZmE4YS00MmZkLTljOTEtZDA0MThiMTQ4MTI3In19.Zxwh0E605zOLOLNxOEUV_tLXX4XD1FB4AHxPOpTrz-o"
            };

            using (var producer = new ProducerBuilder<Null, string>(config).Build())
            {
                var result = await producer.ProduceAsync("logs", new Message<Null, string> { Value = "a log message" });
            }
        }
    }
}