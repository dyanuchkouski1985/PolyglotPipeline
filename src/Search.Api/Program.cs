using Elastic.Clients.Elasticsearch;
using MongoDB.Driver;
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

app.Run();
