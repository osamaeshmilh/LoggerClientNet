namespace LoggerClient.Configurations;

public class LoggerClientConfiguration
{
    public LoggerClientConfiguration(string appToken, string kafkaBootstrapServers, string kafkaTopic,
        string authEndpoint)
    {
        AppToken = appToken;
        KafkaBootstrapServers = kafkaBootstrapServers;
        KafkaTopic = kafkaTopic;
        AuthEndpoint = authEndpoint;
    }

    public string AppToken { get; }
    public string KafkaBootstrapServers { get; }
    public string KafkaTopic { get; }
    public string AuthEndpoint { get; }
}