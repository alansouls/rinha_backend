using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RinhaBackend.Shared.Messaging.RabbitMq;

internal class RabbitMqChannelProvider
{
    private readonly RabbitMqOptions _options;
    
    private readonly Dictionary<string, IChannel> _channels = [];
    
    public RabbitMqChannelProvider(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }
    
    public async Task<IChannel> GetChannel(string channelKey)
    {
        if (_channels.GetValueOrDefault(channelKey, null!) is { } channel)
        {
            return channel;
        }
        
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = "guest",
            Password = "guest"
        };

        var connection = await factory.CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();
        _channels[channelKey] = channel;
        await channel.QueueDeclareAsync(channelKey,
            durable: true,
            exclusive: false,
            autoDelete: true);

        return channel;
    }
}