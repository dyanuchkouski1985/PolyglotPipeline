using System.Text.RegularExpressions;
using Elastic.Clients.Elasticsearch;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Contracts;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration["Mongo:Database"]));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));
builder.Services.AddSingleton(_ =>
    new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(builder.Configuration["Elasticsearch:ConnectionString"]!))));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/search/mongo", async (string q, IMongoDatabase mongoDatabase) =>
{
    var filter = Builders<TextDocument>.Filter.Regex(
        d => d.Text, new BsonRegularExpression(Regex.Escape(q), "i"));

    var results = await mongoDatabase.GetCollection<TextDocument>(TextDocument.CollectionName)
        .Find(filter)
        .ToListAsync();

    return Results.Ok(results);
});

app.MapGet("/search/redis", async (string q, IConnectionMultiplexer redis) =>
{
    var database = redis.GetDatabase();
    var server = redis.GetServer(redis.GetEndPoints().Single());

    var results = new List<TextDocument>();
    foreach (var key in server.Keys(pattern: $"{TextSubmitted.RedisKeyPrefix}*"))
    {
        var hash = await database.HashGetAllAsync(key);
        var text = (string?)hash.FirstOrDefault(h => h.Name == nameof(TextSubmitted.Text)).Value;
        if (text is null || !text.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var createdAt = (string)hash.First(h => h.Name == nameof(TextSubmitted.CreatedAt)).Value!;
        results.Add(new TextDocument
        {
            Id = key.ToString()![TextSubmitted.RedisKeyPrefix.Length..],
            Text = text,
            CreatedAt = DateTimeOffset.Parse(createdAt)
        });
    }

    return Results.Ok(results);
});

app.Run();
