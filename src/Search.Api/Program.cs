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

app.Run();
