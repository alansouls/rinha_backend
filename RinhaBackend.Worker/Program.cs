
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;
using RinhaBackend.Shared.Messaging.RabbitMq.Extensions;
using RinhaBackend.Shared.Models;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRabbitMqMessaging();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();

internal class Worker : BackgroundService
{
    private readonly IConsumer _consumer;

    public Worker(IConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            var message = await _consumer.ConsumeAsync<Todo>(AppJsonSerializerContext.Default.Todo);
            
            Console.WriteLine($"Received message: {message.Id} - {message.Title} - {message.DueBy}");
        }
    }
}