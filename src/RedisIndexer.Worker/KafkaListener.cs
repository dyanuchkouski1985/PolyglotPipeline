using System.Text.Json;
using Confluent.Kafka;
using Shared.Contracts;

namespace RedisIndexer.Worker;

public class KafkaListener(
    IConsumer<Null, string> consumer,
    TextSubmittedHandler handler,
    ILogger<KafkaListener> logger) : BackgroundService
{
    public const string GroupId = "redis-indexer";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(TextSubmitted.KafkaTopicName);
        logger.LogInformation("Listening for {Message} on Kafka topic {Topic}", nameof(TextSubmitted), TextSubmitted.KafkaTopicName);

        return Task.Run(async () =>
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);
                    var message = JsonSerializer.Deserialize<TextSubmitted>(result.Message.Value);
                    if (message is not null)
                    {
                        await handler.HandleAsync(message, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during graceful shutdown.
            }
            finally
            {
                consumer.Close();
            }
        }, stoppingToken);
    }
}
