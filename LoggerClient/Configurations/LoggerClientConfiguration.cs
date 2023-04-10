namespace LoggerClient.Configurations
{
    public class LoggerClientConfiguration
    {
        public string AppToken { get; private set; }
        public string KafkaBootstrapServers { get; private set; }
        public string KafkaTopic { get; private set; }

        public LoggerClientConfiguration(string appToken, string kafkaBootstrapServers, string kafkaTopic)
        {
            AppToken = appToken;
            KafkaBootstrapServers = kafkaBootstrapServers;
            KafkaTopic = kafkaTopic;
        }
    }
}