using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;
using RinhaBackend.Shared.Domain.Outbox;
using RinhaBackend.Shared.Dtos.Payments;
using RinhaBackend.Shared.Dtos.Payments.Requests;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;
using RinhaBackend.Shared.Messaging.Udp.Extensions;
using RinhaBackend.Shared.ThirdParty;
using RinhaBackend.Shared.ThirdParty.Extensions;
using RinhaBackend.Worker.Options;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Services.AddUdpMessaging("localhost", 9840, 9841, 9842, ["rinhabackend.api1", "rinhabackend.api2"]);
builder.Services.AddHostedService<Worker>();
builder.Services.AddThirdPartyPaymentProcessor(builder.Configuration);
builder.Services.Configure<WorkerOptions>(s => builder.Configuration.GetSection("WorkerOptions").Bind(s));

IHost host = builder.Build();
host.Run();

internal class Worker : BackgroundService
{
    private readonly IConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly IMessenger _messenger;

    public Worker(IConsumer consumer, IServiceProvider serviceProvider, IOptions<WorkerOptions> options,
        ILogger<Worker> logger, IMessenger messenger)
    {
        _consumer = consumer;
        _serviceProvider = serviceProvider;
        _messenger = messenger;
        _semaphoreSlim = new SemaphoreSlim(options.Value.MaxConcurrentTasks);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await foreach (var message in _consumer
                                   .ConsumeAsync<PostPaymentDto>(AppJsonSerializerContext.Default.PostPaymentDto,
                                       stoppingToken))
                {
                    _ = Task.Run(async () => await ProcessMessage(stoppingToken, message), stoppingToken);
                }
            }
            catch
            {
                //Ignore
            }
        }
    }

    private async Task ProcessMessage(CancellationToken stoppingToken, TypedOutboxMessage<PostPaymentDto> message)
    {
        await _semaphoreSlim.WaitAsync(stoppingToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            try
            {
                var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                var result = await ProcessInboxMessage(paymentService, message.Payload, stoppingToken);
            }
            catch (Exception e)
            {
                //Ignore
            }
        }
        catch (Exception ex)
        {
            // Ignore
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task<bool> ProcessInboxMessage(IPaymentService paymentService, PostPaymentDto payload,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.ProcessPaymentAsync(payload.CorrelationId, payload.Amount, cancellationToken);

        if (result is null)
        {
            return false;
        }

        await _messenger.BroadcastAsync(new PaymentUpdated()
        {
            TimeStamp = result.Value.Item2,
            Service = result.Value.Item1,
            Amount = payload.Amount,
            Id = payload.CorrelationId
        }, AppJsonSerializerContext.Default.PaymentUpdated, cancellationToken);

        return true;
    }
}