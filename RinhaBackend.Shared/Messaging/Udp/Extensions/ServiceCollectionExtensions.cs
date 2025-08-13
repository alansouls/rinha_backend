using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RinhaBackend.Shared.Messaging.Interfaces;

namespace RinhaBackend.Shared.Messaging.Udp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUdpMessaging(this IServiceCollection services, string sendHostName,
        int sendPort, int broadcastPort, int listenerPort, string[] broadcastHosts)
    {
        services.AddSingleton<IMessenger, UdpMessenger>(s => new UdpMessenger(sendHostName, sendPort, broadcastPort, broadcastHosts));
        services.AddSingleton<IConsumer, UdpConsumer>(s =>
            new UdpConsumer(listenerPort, s.GetRequiredService<ILogger<UdpConsumer>>()));

        return services;
    }
}