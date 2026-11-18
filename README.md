# SDDP-LoggerClientNet

[![NuGet](https://img.shields.io/nuget/v/SDDP-LoggerClientNet.svg)](https://www.nuget.org/packages/SDDP-LoggerClientNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

An ASP.NET Core middleware that logs every HTTP request and response and publishes the log entries to Apache Kafka using [Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet).

## Features

- Captures method, path, query string, status code, remote IP, duration, headers, and cookie names for each request
- Captures request bodies, and response bodies for `application/json` responses
- Publishes each entry to a Kafka topic
- Redacts sensitive headers (`Authorization`, `Cookie`, `Set-Cookie`, `X-Api-Key`) and cookie values
- Authenticates each application with a token
- Does not change the response your application sends to clients

## Requirements

- .NET 8.0, .NET 9.0, or .NET 10.0 (.NET 6 and 7 are not supported)
- ASP.NET Core
- A reachable Kafka broker
- A reachable authentication endpoint that accepts your application token

## Installation

```bash
dotnet add package SDDP-LoggerClientNet
```

## Quick start

Register the services in `Program.cs`:

```csharp
using LoggerClient.Authentication;
using LoggerClient.Configurations;
using LoggerClient.Kafka;
using LoggerClient.Middleware;

var builder = WebApplication.CreateBuilder(args);

var section = builder.Configuration.GetSection("LoggerClient");

var loggerConfig = new LoggerClientConfiguration(
    appToken: section["AppToken"] ?? throw new InvalidOperationException("LoggerClient:AppToken is missing."),
    kafkaBootstrapServers: section["KafkaBootstrapServers"] ?? throw new InvalidOperationException("LoggerClient:KafkaBootstrapServers is missing."),
    kafkaTopic: section["KafkaTopic"] ?? throw new InvalidOperationException("LoggerClient:KafkaTopic is missing."),
    authEndpoint: section["AuthEndpoint"] ?? throw new InvalidOperationException("LoggerClient:AuthEndpoint is missing."));

builder.Services.AddSingleton(loggerConfig);
builder.Services.AddSingleton<ApplicationAuthenticator>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

var app = builder.Build();

app.UseMiddleware<HttpLoggingMiddleware>();

app.MapGet("/", () => "Hello World");

app.MapPost("/echo", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    return Results.Text(await reader.ReadToEndAsync(), "text/plain");
});

app.MapGet("/json", () => Results.Json(new { message = "hello", ok = true }));

app.Run("http://localhost:5000");
```

Register the middleware before the endpoints it should log.

## Configuration

Add the settings to `appsettings.json`:

```json
{
  "LoggerClient": {
    "AppToken": "your-application-token",
    "KafkaBootstrapServers": "localhost:9092",
    "KafkaTopic": "http-logs",
    "AuthEndpoint": "https://your-auth-server"
  }
}
```

| Setting | Description |
|---|---|
| `AppToken` | Token identifying your application. Sent to the authentication endpoint. |
| `KafkaBootstrapServers` | Kafka broker address, for example `localhost:9092`. |
| `KafkaTopic` | Kafka topic that receives the log entries. |
| `AuthEndpoint` | Base URL of the authentication service. Use HTTPS in production. |

All four settings are required. The application fails at startup with a clear error if one is missing.

> **Do not commit real tokens to source control.** In development, use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets). In production, use environment variables or your secret manager. Environment variables use double underscores for nested keys, for example `LoggerClient__AppToken`.

> **Verify:** the property names above must match the properties on `LoggerClientConfiguration`. Adjust them if the names differ.

## Log entry fields

Each entry is serialized to JSON and published with these fields:

| Field | Description |
|---|---|
| `request_timestamp` | UTC time the request was received |
| `application_id` | ID of the application, returned by the authentication endpoint |
| `http_method` | HTTP method, such as `GET` or `POST` |
| `request_url` | Request path |
| `request_url_parameters` | Query string |
| `http_status_code` | Response status code |
| `remote_ip_address` | Client IP address |
| `duration` | Time spent handling the request, in milliseconds |
| `request_headers` | Request headers as JSON, with sensitive values redacted |
| `response_headers` | Response headers as JSON, with sensitive values redacted |
| `request_cookies` | Cookie names with values redacted |
| `request_body` | Full request body |
| `response_body` | Response body, only for `application/json` responses |

## Security considerations

- **Request bodies are logged in full.** If a request contains passwords, tokens, or personal data, that data is sent to Kafka. Exclude those endpoints from logging or avoid sending secrets to them.
- **Redaction covers only the listed headers.** Add any other sensitive header names to your own copy of the middleware if needed.
- **The application token is sent to the authentication endpoint** in an `Authorization: Bearer` header. Use HTTPS for `AuthEndpoint`.

## Behavior

- **Authentication.** The middleware looks up the application ID on the first request and caches it for the life of the process. A revoked token keeps working until the application restarts.
- **Authentication failures.** If the endpoint is unavailable or rejects the token, requests pass through unlogged. The middleware retries authentication no more than once every 30 seconds.
- **Logging failures.** Errors while building or publishing a log entry are written to the console and do not affect the response.
- **Kafka publishing.** Entries are published from the request pipeline. Confirm how your `KafkaProducer` handles broker outages before relying on it in production.

## Contributing

Issues and pull requests are welcome at [github.com/osamaeshmilh/LoggerClientNet](https://github.com/osamaeshmilh/LoggerClientNet).

## License

Released under the [MIT License](LICENSE). Copyright (c) 2023-2026 Osama Eshmilh.