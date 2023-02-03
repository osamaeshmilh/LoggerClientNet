using Confluent.Kafka;

namespace LoggerClient
{
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
}