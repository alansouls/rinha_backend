using RabbitMQ.Client.Exceptions;
using RinhaBackend.Shared.Dtos.Payments;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;
using RinhaBackend.Shared.Utils;

namespace RinhaBackend.Api;

public class DbUpdater : BackgroundService
{
    private readonly IConsumer _consumer;
    private readonly MyMemDb _db;
    private readonly ILogger<DbUpdater> _logger;

    public DbUpdater(IConsumer consumer, MyMemDb db, ILogger<DbUpdater> logger)
    {
        _consumer = consumer;
        _db = db;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            try
            {
                await foreach (var message in _consumer
                                   .ConsumeAsync<PaymentUpdated>(AppJsonSerializerContext.Default.PaymentUpdated,
                                       stoppingToken))
                {
                    _db.Insert(message.Payload);
                }
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogError(ex, "Broker unreachable. Retrying in 5 seconds...");
                await Task.Delay(100, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while consuming messages: {Message}", ex.Message);
            }
        }
    }
}