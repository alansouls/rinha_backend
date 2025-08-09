using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RinhaBackend.Shared.ThirdParty.Options;

namespace RinhaBackend.Shared.ThirdParty.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThirdPartyPaymentProcessor(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IServiceHealthProvider, ServiceHealthProvider>();
        var options = configuration.GetSection("PaymentService").Get<PaymentServiceOptions>()!;
        services.AddHttpClient("PaymentDefault",
            client =>
            {
                client.BaseAddress = new Uri(options.DefaultUrl);
                client.Timeout = options.DefaultTimeout;
            });
        services.AddHttpClient("PaymentFallback",
            client =>
            {
                client.BaseAddress = new Uri(options.FallbackUrl);
                client.Timeout = options.FallbackTimeout;
            });
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}