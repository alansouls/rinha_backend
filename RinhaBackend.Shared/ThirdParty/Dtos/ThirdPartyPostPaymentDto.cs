namespace RinhaBackend.Shared.ThirdParty.Dtos;

public class ThirdPartyPostPaymentDto
{
    public Guid CorrelationId { get; set; }
    
    public decimal Amount { get; set; }
    
    public DateTimeOffset RequestedAt { get; set; }
}