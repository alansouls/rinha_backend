using RinhaBackend.Shared.Domain.Payments;

namespace RinhaBackend.Shared.ThirdParty;

public interface IServiceHealthProvider
{
    PaymentServiceEnum GetPreferredService();
}