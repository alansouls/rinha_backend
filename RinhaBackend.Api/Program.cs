using Microsoft.EntityFrameworkCore;
using RinhaBackend.Shared.Data;
using RinhaBackend.Shared.Domain.Payments;
using RinhaBackend.Shared.Dtos.Payments.Requests;
using RinhaBackend.Shared.Dtos.Payments.Responses;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;
using RinhaBackend.Shared.Messaging.RabbitMq.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddDbContext<RinhaContext>(s =>
    s.UseNpgsql(builder.Configuration.GetConnectionString("RinhaDatabase")));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddRabbitMqMessaging();

var app = builder.Build();

app.MapPost("/payments", async
    (PostPaymentDto message, IMessenger messenger, CancellationToken cancellationToken) =>
{
    await messenger.SendAsync(message, AppJsonSerializerContext.Default.PostPaymentDto, message.CorrelationId,
        cancellationToken);
});

app.MapGet("/payments-summary", async
    (DateTimeOffset? from, DateTimeOffset? to, RinhaContext context, CancellationToken cancellationToken) =>
{
    var query = context.Set<PaymentLog>().AsQueryable();

    if (from.HasValue)
    {
        query = query.Where(p => p.TimeStamp >= from.Value);
    }

    if (to.HasValue)
    {
        query = query.Where(p => p.TimeStamp <= to.Value);
    }

    var result = await query.GroupBy(p => p.ServiceEnum)
        .Select(gp => new
        { 
            Service = gp.Key,
            Summary = new PaymentServiceSummaryDto()
            {
                TotalAmount = gp.Sum(p => p.Amount),
                TotalRequests = gp.Count()
            }
        }).ToDictionaryAsync(gp => gp.Service, gp => gp.Summary, cancellationToken);

    return new PaymentSummaryDto()
    {
        Default = result.GetValueOrDefault(PaymentServiceEnum.Default, new PaymentServiceSummaryDto()),
        Fallback = result.GetValueOrDefault(PaymentServiceEnum.Fallback, new PaymentServiceSummaryDto())
    };
});

app.Run();