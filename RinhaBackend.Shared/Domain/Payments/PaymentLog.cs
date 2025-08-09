namespace RinhaBackend.Shared.Domain.Payments;

public class PaymentLog
{
    public int Id { get; set; }
    
    public Guid CorrelationId { get; set; }
    
    public PaymentServiceEnum ServiceEnum { get; set; }
    
    public decimal Amount { get; set; }
    
    public DateTimeOffset TimeStamp { get; set; }
}