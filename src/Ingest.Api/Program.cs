using Ingest.Api;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration["Mongo:Database"]));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/ingest", async (string text, IMongoDatabase mongoDatabase) =>
{
    var document = new TextDocument
    {
        Id = Guid.NewGuid().ToString(),
        Text = text,
        CreatedAt = DateTimeOffset.UtcNow
    };

    await mongoDatabase.GetCollection<TextDocument>(TextDocument.CollectionName).InsertOneAsync(document);

    return Results.Ok(document);
});

app.Run();
