using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RinhaBackend.Shared.Messaging.Interfaces;

namespace RinhaBackend.Shared.Messaging.RabbitMq;

public class RabbitMqMessenger : IMessenger
{
    private readonly Dictionary<string, IChannel> _channels = [];
   
    private readonly RabbitMqOptions _options;
    
    public RabbitMqMessenger(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }
    
    public async Task SendAsync<T>(T message, JsonTypeInfo<T> jsonTypeInfo) where T : class
    {
        var channel = await GetChannel<T>();

        var body = JsonSerializer.SerializeToUtf8Bytes(message, jsonTypeInfo);

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: typeof(T).Name, body.AsMemory());
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
        channel =  await connection.CreateChannelAsync();
        _channels[typeof(T).Name] = channel;
        await channel.QueueDeclareAsync(typeof(T).Name,
            durable: true,
            exclusive: false,
            autoDelete: true);

        return channel;
    }
}