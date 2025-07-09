using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Exceptions;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;
using RinhaBackend.Shared.Messaging.RabbitMq.Extensions;
using RinhaBackend.Shared.Models;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
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
            try
            {
                await foreach (var message in _consumer.ConsumeAsync<Todo>(AppJsonSerializerContext.Default.Todo))
                {
                    Console.WriteLine($"Received message: {message.Id} - {message.Title} - {message.DueBy}");
                }
            }
            catch (BrokerUnreachableException ex)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}