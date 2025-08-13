using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using RinhaBackend.Shared.Messaging.Interfaces;

namespace RinhaBackend.Shared.Messaging.Udp;

public class UdpMessenger : IMessenger, IDisposable
{
    private const string WorkerHostName = "rinhabackend.worker";
    private const int WorkerPort = 9841;
    private readonly UdpClient _udpClient;
    private readonly int _broadcastPort;
    private readonly string[] _broadcastHosts;

    public UdpMessenger(string sendHostName, int sendPort, int broadcastPort, string[] broadcastHosts)
    {
        _udpClient = new(sendHostName, sendPort);
        _broadcastPort = broadcastPort;
        _broadcastHosts = broadcastHosts;
    }

    public async Task SendAsync<T>(T message, JsonTypeInfo<T> jsonTypeInfo, Guid? messageId,
        CancellationToken cancellationToken) where T : class
    {
        var serializedMessage = JsonSerializer.SerializeToUtf8Bytes(message, jsonTypeInfo);

        await _udpClient.SendAsync(serializedMessage, cancellationToken);
    }

    public async Task BroadcastAsync<T>(T message, JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken) where T : class
    {
        var serializedMessage = JsonSerializer.SerializeToUtf8Bytes(message, jsonTypeInfo);

        foreach (var broadcastHost in _broadcastHosts)
        {
            using var broadCastClient = new UdpClient(broadcastHost, _broadcastPort);

            await broadCastClient.SendAsync(serializedMessage, cancellationToken);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _udpClient.Dispose();
    }
}