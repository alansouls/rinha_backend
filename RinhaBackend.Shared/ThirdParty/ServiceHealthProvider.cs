using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using RinhaBackend.Shared.Domain.Payments;
using RinhaBackend.Shared.JsonSerialization;

namespace RinhaBackend.Shared.ThirdParty;

public class ServiceHealthProvider : IServiceHealthProvider, IHostedService
{
    private bool _defaultServiceHealthy = true;
    private bool _fallbackServiceHealthy = true;
    private PaymentServiceEnum _preferredService = PaymentServiceEnum.Default;
    private readonly HttpClient _defaultHttpClient;
    private readonly HttpClient _fallbackHttpClient;
    private PeriodicTimer? _timer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ServiceHealthProvider(IHttpClientFactory httpClientFactory)
    {
        _defaultHttpClient = httpClientFactory.CreateClient("PaymentDefault");
        _fallbackHttpClient = httpClientFactory.CreateClient("PaymentFallback");
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        
        _ = Task.Run(() => CheckHealthyAsync(_timer, _cancellationTokenSource.Token), cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _cancellationTokenSource.CancelAsync();
    }

    public async Task CheckHealthyAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await timer.WaitForNextTickAsync(cancellationToken);

            _defaultServiceHealthy = false;
            _fallbackServiceHealthy = false;
            var defaultResponseTime = int.MaxValue;
            var fallbackResponseTime = int.MaxValue;
            try
            {
                var defaultResponse = await _defaultHttpClient.GetFromJsonAsync("service-health", AppJsonSerializerContext.Default.ServiceHealthResponseDto, cancellationToken);
                if (defaultResponse is not null)
                {
                    _defaultServiceHealthy = defaultResponse.IsFailing;
                    defaultResponseTime = defaultResponse.MinResponseTime;
                }
            }
            catch
            {
                // ignored
            }

            if (_defaultServiceHealthy)
            {
                _preferredService = PaymentServiceEnum.Default;
            }
            
            try
            {
                var fallbackResponse = await _fallbackHttpClient.GetFromJsonAsync("service-health", AppJsonSerializerContext.Default.ServiceHealthResponseDto, cancellationToken);
                if (fallbackResponse is not null)
                {
                    _fallbackServiceHealthy = fallbackResponse.IsFailing;
                    fallbackResponseTime = fallbackResponse.MinResponseTime;
                }
            }
            catch
            {
                // ignored
            }

            if (_fallbackServiceHealthy && !_defaultServiceHealthy)
            {
                _preferredService = PaymentServiceEnum.Fallback;
            }
            else if (_fallbackServiceHealthy && _defaultServiceHealthy)
            {
                _preferredService = fallbackResponseTime > defaultResponseTime
                    ? PaymentServiceEnum.Fallback
                    : PaymentServiceEnum.Default;
            }
            else
            {
                _preferredService = PaymentServiceEnum.Default;
            }
        }
    }

    public PaymentServiceEnum GetPreferredService()
    {
        return _preferredService;
    }
}