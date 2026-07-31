namespace Shared.Contracts;

public record TextSubmitted
{
    public const string RabbitMqExchangeName = "text-submitted";

    public const string KafkaTopicName = "text-submitted";

    public required string Id { get; init; }

    public required string Text { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
