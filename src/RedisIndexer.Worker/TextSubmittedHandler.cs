using Shared.Contracts;
using StackExchange.Redis;

namespace RedisIndexer.Worker;

public class TextSubmittedHandler(IConnectionMultiplexer redis, ILogger<TextSubmittedHandler> logger)
{
    public async Task HandleAsync(TextSubmitted message, CancellationToken cancellationToken)
    {
        var database = redis.GetDatabase();
        await database.HashSetAsync(TextSubmitted.RedisKeyPrefix + message.Id,
        [
            new HashEntry(nameof(TextSubmitted.Text), message.Text),
            new HashEntry(nameof(TextSubmitted.CreatedAt), message.CreatedAt.ToString("o"))
        ]);

        logger.LogInformation("Wrote {Message} {Id} to Redis: {Text}", nameof(TextSubmitted), message.Id, message.Text);
    }
}
