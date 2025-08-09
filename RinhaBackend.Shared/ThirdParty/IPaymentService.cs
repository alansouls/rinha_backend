using RinhaBackend.Shared.Domain.Payments;

namespace RinhaBackend.Shared.ThirdParty;

public interface IPaymentService
{
    Task<(PaymentServiceEnum, DateTimeOffset)?> ProcessPaymentAsync(Guid correlationId, decimal amount,
        CancellationToken cancellationToken);
}