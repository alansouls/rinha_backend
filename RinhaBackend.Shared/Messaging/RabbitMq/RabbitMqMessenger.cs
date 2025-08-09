using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RinhaBackend.Shared.Data;
using RinhaBackend.Shared.Domain.Common;
using RinhaBackend.Shared.Domain.Outbox;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;

namespace RinhaBackend.Shared.Messaging.RabbitMq;

public class RabbitMqMessenger : IMessenger
{
    private readonly Dictionary<string, IChannel> _channels = [];
    private readonly IServiceProvider _serviceProvider;

    private readonly RabbitMqOptions _options;

    public RabbitMqMessenger(IOptions<RabbitMqOptions> options, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public async Task SendAsync<T>(T message, JsonTypeInfo<T> jsonTypeInfo, Guid? messageId, CancellationToken cancellationToken)
        where T : class
    {
        var channel = await GetChannel<T>();

        var created = DateTimeOffset.UtcNow;

        var outboxMessage = new OutboxMessage()
        {
            Id = messageId ?? Guid.NewGuid(),
            Type = typeof(T).Name,
            Payload = JsonSerializer.Serialize(message, jsonTypeInfo),
            CreatedAt = created,
            UpdatedAt = created,
            Retries = 0,
            State = OutboxState.Queued
        };
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RinhaContext>();

        await context.AddAsync(outboxMessage, cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(outboxMessage, AppJsonSerializerContext.Default.OutboxMessage);

        try
        {
            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: typeof(T).Name, body.AsMemory(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not TaskCanceledException and not OperationCanceledException)
        {
            outboxMessage.State = OutboxState.ReadyToRetry;
        }
        
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<IChannel> GetChannel<T>() where T : class
    {
        if (_channels.GetValueOrDefault(typeof(T).Name, null!) is { } channel)
        {
            return channel;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port
        };

        var connection = await factory.CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();
        _channels[typeof(T).Name] = channel;
        await channel.QueueDeclareAsync(typeof(T).Name,
            durable: true,
            exclusive: false,
            autoDelete: true);

        return channel;
    }
}