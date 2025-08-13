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
builder.Services.AddUdpMessaging("localhost", 9840, 9841, 9842, ["rinhabackend.api1", "rinhabackend.api2", "rinhabackend.api3", "rinhabackend.api4"]);
builder.Services.AddHostedService<Worker>();
builder.Services.AddThirdPartyPaymentProcessor(builder.Configuration);
builder.Services.Configure<WorkerOptions>(s => builder.Configuration.GetSection("WorkerOptions").Bind(s));

IHost host = builder.Build();
host.Run();

internal class Worker : BackgroundService
{
    private readonly IConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    private const int _maxRetries = 3;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly ILogger<Worker> _logger;
    private readonly IMessenger _messenger;

    public Worker(IConsumer consumer, IServiceProvider serviceProvider, IOptions<WorkerOptions> options,
        ILogger<Worker> logger, IMessenger messenger)
    {
        _consumer = consumer;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messenger = messenger;
        _semaphoreSlim = new SemaphoreSlim(options.Value.MaxConcurrentTasks);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            try
            {
                await foreach (var message in _consumer
                                   .ConsumeAsync<PostPaymentDto>(AppJsonSerializerContext.Default.PostPaymentDto,
                                       stoppingToken))
                {
                    _logger.LogInformation(
                        "Received message with Id: {Id}, CorrelationId: {CorrelationId}, Amount: {Amount}",
                        message.Id, message.Payload.CorrelationId, message.Payload.Amount);
                    _ = Task.Run(async () => await ProcessMessage(stoppingToken, message), stoppingToken);
                }
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogError(ex, "Broker unreachable. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while consuming messages: {Message}", ex.Message);
            }
        }
    }

    private async Task ProcessMessage(CancellationToken stoppingToken, TypedOutboxMessage<PostPaymentDto> message)
    {
        _logger.LogInformation("About to wait for semaphore");
        await _semaphoreSlim.WaitAsync(stoppingToken);
        _logger.LogInformation("In semaphore");
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
                _logger.LogError(e, "Error processing payment for CorrelationId: {CorrelationId}, Amount: {Amount}",
                    message.Payload.CorrelationId, message.Payload.Amount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An error occurred while processing message with Id: {Id}, CorrelationId: {CorrelationId}, Amount: {Amount}",
                message.Id, message.Payload.CorrelationId, message.Payload.Amount);
        }
        finally
        {
            _logger.LogDebug("Releasing semaphore for message with Id: {Id}", message.Id);
            _semaphoreSlim.Release();
        }
    }

    private async Task<bool> ProcessInboxMessage(IPaymentService paymentService, PostPaymentDto payload,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.ProcessPaymentAsync(payload.CorrelationId, payload.Amount, cancellationToken);

        if (result is null)
        {
            _logger.LogError("Payment processing failed for CorrelationId: {CorrelationId}, Amount: {Amount}",
                payload.CorrelationId, payload.Amount);
            return false;
        }

        _logger.LogDebug(
            "Payment processed successfully for CorrelationId: {CorrelationId}, Amount: {Amount}, Service: {Service}, TimeStamp: {TimeStamp}",
            payload.CorrelationId, payload.Amount, result.Value.Item1, result.Value.Item2);

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