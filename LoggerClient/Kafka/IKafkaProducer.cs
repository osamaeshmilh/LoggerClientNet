using Confluent.Kafka;
using LoggerClient.Configurations;
using LoggerClient.Models;
using Newtonsoft.Json;

namespace LoggerClient.Kafka;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaProducer(LoggerClientConfiguration configuration)
    {
        var config = new ProducerConfig { BootstrapServers = configuration.KafkaBootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
        _topic = configuration.KafkaTopic;
    }

    public async Task ProduceAsync(LogEntry logEntry)
    {
        var messageKey = logEntry.request_timestamp.ToString("O") + "-" + Guid.NewGuid();
        var messageValue = JsonConvert.SerializeObject(logEntry);

        await _producer.ProduceAsync(_topic, new Message<string, string>
        {
            Key = messageKey,
            Value = messageValue
        });
    }
}