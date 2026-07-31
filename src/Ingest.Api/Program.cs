using System.Text.Json;
using Confluent.Kafka;
using Ingest.Api;
using MongoDB.Driver;
using RabbitMQ.Client;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration["Mongo:Database"]));
builder.Services.AddSingleton(new ConnectionFactory
{
    Uri = new Uri(builder.Configuration["RabbitMq:ConnectionString"]!)
});
builder.Services.AddSingleton(_ =>
    new ProducerBuilder<Null, string>(new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    }).Build());

const string RabbitMqBroker = "rabbitmq";
const string KafkaBroker = "kafka";

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/ingest", async (string text, string broker, IMongoDatabase mongoDatabase, ConnectionFactory rabbitMqConnectionFactory, IProducer<Null, string> kafkaProducer) =>
{
    if (broker is not (RabbitMqBroker or KafkaBroker))
    {
        return Results.BadRequest($"broker must be '{RabbitMqBroker}' or '{KafkaBroker}', got '{broker}'.");
    }

    var document = new TextDocument
    {
        Id = Guid.NewGuid().ToString(),
        Text = text,
        CreatedAt = DateTimeOffset.UtcNow
    };

    await mongoDatabase.GetCollection<TextDocument>(TextDocument.CollectionName).InsertOneAsync(document);

    var message = new TextSubmitted
    {
        Id = document.Id,
        Text = document.Text,
        CreatedAt = document.CreatedAt
    };

    if (broker == RabbitMqBroker)
    {
        await using var connection = await rabbitMqConnectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(TextSubmitted.RabbitMqExchangeName, ExchangeType.Fanout, durable: true);

        await channel.BasicPublishAsync(
            exchange: TextSubmitted.RabbitMqExchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent },
            body: JsonSerializer.SerializeToUtf8Bytes(message));
    }
    else
    {
        await kafkaProducer.ProduceAsync(
            TextSubmitted.KafkaTopicName,
            new Message<Null, string> { Value = JsonSerializer.Serialize(message) });
    }

    return Results.Ok(document);
});

app.Run();
