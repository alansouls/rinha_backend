using RinhaBackend.Api;
using RinhaBackend.Shared.Domain.Payments;
using RinhaBackend.Shared.Dtos.Payments.Requests;
using RinhaBackend.Shared.Dtos.Payments.Responses;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;
using RinhaBackend.Shared.Messaging.Udp.Extensions;
using RinhaBackend.Shared.Utils;

var builder = WebApplication.CreateSlimBuilder(args);

GC.TryStartNoGCRegion(10 * 1024 * 1024); // Try to start a no-GC region of 10 MB

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddUdpMessaging("rinhabackend.worker", 9842, 9830, 9841, []);
builder.Services.AddSingleton<MyMemDb>();
builder.Services.AddHostedService<DbUpdater>();

var app = builder.Build();

app.MapPost("/payments",
    (PostPaymentDto message, IMessenger messenger) =>
    {
        _ = Task.Run(async () => await messenger.SendAsync(message, AppJsonSerializerContext.Default.PostPaymentDto,
            message.CorrelationId, CancellationToken.None));
    });

app.MapGet("/payments-summary", (DateTimeOffset? from, DateTimeOffset? to, MyMemDb db) =>
{
    var result = db.GetPaymentDtos(from, to).GroupBy(p => p.Service)
        .Select(gp => new
        {
            Service = gp.Key,
            Summary = new PaymentServiceSummaryDto()
            {
                TotalAmount = gp.Sum(p => p.Amount),
                TotalRequests = gp.Count()
            }
        }).ToDictionary(gp => gp.Service, gp => gp.Summary);

    return new PaymentSummaryDto()
    {
        Default = result.GetValueOrDefault(PaymentServiceEnum.Default, new PaymentServiceSummaryDto()),
        Fallback = result.GetValueOrDefault(PaymentServiceEnum.Fallback, new PaymentServiceSummaryDto())
    };
});

app.Run();