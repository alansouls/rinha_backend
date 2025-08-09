using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;
using RinhaBackend.Shared.Data;
using RinhaBackend.Shared.Domain.Inbox;
using RinhaBackend.Shared.Domain.Outbox;
using RinhaBackend.Shared.Domain.Payments;
using RinhaBackend.Shared.Dtos.Payments.Requests;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;
using RinhaBackend.Shared.Messaging.RabbitMq.Extensions;
using RinhaBackend.Shared.ThirdParty;
using RinhaBackend.Shared.ThirdParty.Extensions;
using RinhaBackend.Worker.Options;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Services.AddDbContext<RinhaContext>(s =>
    s.UseNpgsql(builder.Configuration.GetConnectionString("RinhaDatabase")));
builder.Services.AddRabbitMqMessaging();
builder.Services.AddHostedService<Worker>();
builder.Services.AddThirdPartyPaymentProcessor(builder.Configuration);
builder.Services.Configure<WorkerOptions>(s => builder.Configuration.GetSection("WorkerOptions").Bind(s));

IHost host = builder.Build();
host.Run();

internal class Worker : BackgroundService
{
    private readonly IConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    private int _maxRetries = 3;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly ILogger<Worker> _logger;

    public Worker(IConsumer consumer, IServiceProvider serviceProvider, IOptions<WorkerOptions> options,
        ILogger<Worker> logger)
    {
        _consumer = consumer;
        _serviceProvider = serviceProvider;
        _logger = logger;
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
                                   .ConsumeAsync<PostPaymentDto>(AppJsonSerializerContext.Default.PostPaymentDto)
                                   .WithCancellation(stoppingToken))
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
            var context = scope.ServiceProvider.GetRequiredService<RinhaContext>();
            var inbox = await context.Set<InboxMessage>()
                .FirstOrDefaultAsync(i => i.OutboxMessageId == message.Id, stoppingToken);

            if (inbox is null)
            {
                var createdAt = DateTimeOffset.UtcNow;
                inbox = new InboxMessage()
                {
                    Id = Guid.NewGuid(),
                    OutboxMessageId = message.Id,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    Retries = 0,
                    State = InboxState.Running
                };
                context.Add(inbox);
            }
            else if (inbox.State == InboxState.ReadyToRetry && inbox.Retries < _maxRetries)
            {
                inbox.Retries++;
                inbox.UpdatedAt = DateTimeOffset.UtcNow;
                inbox.State = InboxState.Running;
            }
            else if (inbox.State == InboxState.ReadyToRetry)
            {
                inbox.State = InboxState.Failed;
            }
            else
            {
                _logger.LogInformation(
                    "Inbox message with Id: {Id} is already in state: {State}. Skipping processing.",
                    inbox.Id, inbox.State);
                return;
            }

            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Processing inbox message with Id: {Id}, CorrelationId: {CorrelationId}, Amount: {Amount}",
                inbox.Id, message.Payload.CorrelationId, message.Payload.Amount);

            try
            {
                var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                var result = await ProcessInboxMessage(paymentService, context, message.Payload, stoppingToken);

                if (result)
                {
                    inbox.State = InboxState.Succeeded;
                }
                else
                {
                    inbox.State = inbox.Retries < _maxRetries ? InboxState.ReadyToRetry : InboxState.Failed;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing payment for CorrelationId: {CorrelationId}, Amount: {Amount}",
                    message.Payload.CorrelationId, message.Payload.Amount);
                inbox.State = inbox.Retries < _maxRetries ? InboxState.ReadyToRetry : InboxState.Failed;
            }
            
            _logger.LogInformation(
                "Updating inbox message with Id: {Id}, State: {State}, Retries: {Retries}",
                inbox.Id, inbox.State, inbox.Retries);
            inbox.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An error occurred while processing message with Id: {Id}, CorrelationId: {CorrelationId}, Amount: {Amount}",
                message.Id, message.Payload.CorrelationId, message.Payload.Amount);
        }
        finally
        {
            _logger.LogInformation("Releasing semaphore for message with Id: {Id}", message.Id);
            _semaphoreSlim.Release();
        }
    }

    private async Task<bool> ProcessInboxMessage(IPaymentService paymentService, RinhaContext context,
        PostPaymentDto payload,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.ProcessPaymentAsync(payload.CorrelationId, payload.Amount, cancellationToken);

        if (result is null)
        {
            _logger.LogInformation("Payment processing failed for CorrelationId: {CorrelationId}, Amount: {Amount}",
                payload.CorrelationId, payload.Amount);
            return false;
        }

        context.Set<PaymentLog>().Add(new PaymentLog()
        {
            CorrelationId = payload.CorrelationId,
            Amount = payload.Amount,
            ServiceEnum = result.Value.Item1,
            TimeStamp = result.Value.Item2
        });

        _logger.LogInformation(
            "Payment processed successfully for CorrelationId: {CorrelationId}, Amount: {Amount}, Service: {Service}, TimeStamp: {TimeStamp}",
            payload.CorrelationId, payload.Amount, result.Value.Item1, result.Value.Item2);

        return true;
    }
}