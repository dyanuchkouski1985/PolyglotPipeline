using MongoDB.Bson.Serialization.Attributes;

namespace Shared.Contracts;

public record TextDocument
{
    public const string CollectionName = "texts";

    [BsonId]
    public required string Id { get; init; }

    public required string Text { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
