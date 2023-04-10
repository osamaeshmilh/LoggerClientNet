# README

This README would normally document whatever steps are necessary to get the
application up and running.

Things you may want to cover:

* .NET version

* System dependencies

* Configuration

To run package you need:

* Set Token and Application code in Startup.cs. Example:
  ```sh
  builder.Services.AddSingleton<LoggerClientConfiguration>(
  new LoggerClientConfiguration(
    "yourAppToken",
    "localhost:9092",
    "http-logs"));
  builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
  
  app.UseHttpLoggingMiddleware();
  ```
