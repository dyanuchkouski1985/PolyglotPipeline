using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;

namespace ElasticIndexer.Worker;

public class RabbitMqListener(
    ConnectionFactory connectionFactory,
    TextSubmittedHandler handler,
    ILogger<RabbitMqListener> logger) : BackgroundService
{
    public const string QueueName = "elastic-indexer";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var (connection, channel) = await ConnectWithRetryAsync(stoppingToken);
        await using var connectionToDispose = connection;
        await using var channelToDispose = channel;

        await channel.ExchangeDeclareAsync(
            TextSubmitted.RabbitMqExchangeName, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            QueueName, TextSubmitted.RabbitMqExchangeName, routingKey: string.Empty, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = JsonSerializer.Deserialize<TextSubmitted>(ea.Body.Span);
            if (message is not null)
            {
                await handler.HandleAsync(message, stoppingToken);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        };

        await channel.BasicConsumeAsync(
            QueueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Listening for {Message} on RabbitMQ queue {Queue}", nameof(TextSubmitted), QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during graceful shutdown.
        }
    }

    private async Task<(IConnection Connection, IChannel Channel)> ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
                var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                return (connection, channel);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Transient (e.g. RabbitMQ isn't ready yet right after `docker compose up`) —
                // log and retry rather than crashing the host.
                logger.LogWarning(ex, "RabbitMQ connection failed, retrying in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
