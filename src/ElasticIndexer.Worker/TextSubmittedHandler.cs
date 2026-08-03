using Shared.Contracts;

namespace ElasticIndexer.Worker;

public class TextSubmittedHandler(ILogger<TextSubmittedHandler> logger)
{
    public Task HandleAsync(TextSubmitted message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received {Message} {Id}: {Text}", nameof(TextSubmitted), message.Id, message.Text);
        return Task.CompletedTask;
    }
}
