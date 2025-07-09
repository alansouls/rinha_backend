using Microsoft.Extensions.DependencyInjection;
using RinhaBackend.Shared.Messaging.Interfaces;

namespace RinhaBackend.Shared.Messaging.RabbitMq.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqChannelProvider>();
        services.AddSingleton<IMessenger, RabbitMqMessenger>();
        services.AddSingleton<IConsumer, RabbitMqConsumer>();
        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration("RabbitMq")
            .ValidateOnStart();
        
        return services;
    }
}