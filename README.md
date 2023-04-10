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
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddHttpKafkaLogger(config =>
    {
      config.ApplicationCode = "your-application-code";
      config.ApplicationToken = "your-application-token";
    });

  }
  ```
