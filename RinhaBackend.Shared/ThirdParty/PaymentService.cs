using System.Net.Http.Json;
using RinhaBackend.Shared.Domain.Payments;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.ThirdParty.Dtos;

namespace RinhaBackend.Shared.ThirdParty;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _defaultHttpClient;
    private readonly HttpClient _fallbackHttpClient;
    private readonly IServiceHealthProvider _serviceHealthProvider;

    public PaymentService(IHttpClientFactory httpClientFactory, IServiceHealthProvider serviceHealthProvider)
    {
        _defaultHttpClient = httpClientFactory.CreateClient("PaymentDefault");
        _fallbackHttpClient = httpClientFactory.CreateClient("PaymentFallback");
        _serviceHealthProvider = serviceHealthProvider;
    }

    public async Task<(PaymentServiceEnum, DateTimeOffset)?> ProcessPaymentAsync(Guid correlationId, decimal amount,
        CancellationToken cancellationToken)
    {
        var preferredService = _serviceHealthProvider.GetPreferredService();

        const int maxRetries = 3;
        int retryCount = 0;

        var timeStamp = DateTimeOffset.UtcNow;
        HttpClient? client = null;
        while (retryCount < maxRetries)
        {
            retryCount++;
            try
            {
                client = client is null
                    ? preferredService == PaymentServiceEnum.Default ? _defaultHttpClient : _fallbackHttpClient
                    : client == _defaultHttpClient
                        ? _fallbackHttpClient
                        : _defaultHttpClient;

                var request = new ThirdPartyPostPaymentDto()
                {
                    CorrelationId = correlationId,
                    Amount = amount,
                    RequestedAt = timeStamp
                };

                var response =
                    await client.PostAsJsonAsync("", request,
                        AppJsonSerializerContext.Default.ThirdPartyPostPaymentDto, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                return (client == _defaultHttpClient ? PaymentServiceEnum.Default : PaymentServiceEnum.Fallback,
                    timeStamp);
            }
            catch
            {
                // Ignored
            }
        }
        return null;
    }
}