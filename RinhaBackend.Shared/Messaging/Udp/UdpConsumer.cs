using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using RinhaBackend.Shared.Domain.Outbox;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;

namespace RinhaBackend.Shared.Messaging.Udp;

public class UdpConsumer : IConsumer
{
    private readonly int _listenerPort;
    private readonly ILogger<UdpConsumer> _logger;
    
    public UdpConsumer(int listenerPort, ILogger<UdpConsumer> logger)
    {
        _listenerPort = listenerPort;
        _logger = logger;
    }

    public async IAsyncEnumerable<TypedOutboxMessage<T>> ConsumeAsync<T>(JsonTypeInfo<T> jsonTypeInfo,
        [EnumeratorCancellation] CancellationToken cancellationToken) where T : class
    {
        using var client = new UdpClient(new IPEndPoint(IPAddress.Any, _listenerPort));
        
        _logger.LogDebug("UdpConsumer started listening on port {Port}", _listenerPort);

        while (!cancellationToken.IsCancellationRequested)
        {
            var datagram = await client.ReceiveAsync(cancellationToken);
            
            _logger.LogDebug("Received datagram of size {Size} from {Endpoint}", 
                datagram.Buffer.Length, datagram.RemoteEndPoint);

            yield return new TypedOutboxMessage<T>()
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Deserialize(datagram.Buffer, jsonTypeInfo)
                          ?? throw new Exception("Deserialized message is null")
            };
        }
    }
}