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
        var options = new PaymentServiceOptions()
        {
            DefaultUrl = configuration.GetValue<string>("PaymentService:DefaultUrl") ?? string.Empty,
            DefaultTimeout =
                configuration.GetValue<TimeSpan>("PaymentService:DefaultTimeout", TimeSpan.FromSeconds(30)),
            FallbackUrl = configuration.GetValue<string>("PaymentService:FallbackUrl") ?? string.Empty,
            FallbackTimeout =
                configuration.GetValue<TimeSpan>("PaymentService:FallbackTimeout", TimeSpan.FromSeconds(30))
        };
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