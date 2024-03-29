using LoggerClient.Models;

namespace LoggerClient.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync(LogEntry logEntry);
}