using RinhaBackend.Shared.Domain.Payments;

namespace RinhaBackend.Shared.Dtos.Payments;

public class PaymentUpdated
{
    public Guid Id { get; set; }
    
    public PaymentServiceEnum Service { get; set; }
    
    public DateTimeOffset TimeStamp { get; set; }
    
    public decimal Amount { get; set; }
}