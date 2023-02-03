using Confluent.Kafka;

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
}