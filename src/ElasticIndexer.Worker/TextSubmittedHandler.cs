using Elastic.Clients.Elasticsearch;
using Shared.Contracts;

namespace ElasticIndexer.Worker;

public class TextSubmittedHandler(ElasticsearchClient elasticsearchClient, ILogger<TextSubmittedHandler> logger)
{
    public async Task HandleAsync(TextSubmitted message, CancellationToken cancellationToken)
    {
        var response = await elasticsearchClient.IndexAsync(
            message,
            request => request.Index(TextSubmitted.ElasticIndexName).Id(message.Id),
            cancellationToken);

        if (response.IsValidResponse)
        {
            logger.LogInformation("Indexed {Message} {Id} into Elasticsearch: {Text}", nameof(TextSubmitted), message.Id, message.Text);
        }
        else
        {
            logger.LogWarning("Failed to index {Message} {Id} into Elasticsearch: {Reason}", nameof(TextSubmitted), message.Id, response.DebugInformation);
        }
    }
}
