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
            DefaultUrl = configuration.GetValue<string>("PaymentService:DefaultUrl") ?? "http://payment-processor-default:8080/payments",
            DefaultTimeout =
                configuration.GetValue("PaymentService:DefaultTimeout", TimeSpan.FromSeconds(5)),
            FallbackUrl = configuration.GetValue<string>("PaymentService:FallbackUrl") ?? "http://payment-processor-fallback:8080/payments",
            FallbackTimeout =
                configuration.GetValue("PaymentService:FallbackTimeout", TimeSpan.FromSeconds(5))
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