using Confluent.Kafka;
using ElasticIndexer.Worker;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new ConnectionFactory
{
    Uri = new Uri(builder.Configuration["RabbitMq:ConnectionString"]!)
});
builder.Services.AddSingleton(_ =>
    new ConsumerBuilder<Null, string>(new ConsumerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        GroupId = KafkaListener.GroupId,
        AutoOffsetReset = AutoOffsetReset.Earliest
    }).Build());

builder.Services.AddSingleton<TextSubmittedHandler>();
builder.Services.AddHostedService<RabbitMqListener>();
builder.Services.AddHostedService<KafkaListener>();

var host = builder.Build();
host.Run();
