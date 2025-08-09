namespace RinhaBackend.Shared.Dtos.Payments.Requests;

public class PostPaymentDto
{
    public Guid CorrelationId { get; set; }
    
    public decimal Amount { get; set; }
}