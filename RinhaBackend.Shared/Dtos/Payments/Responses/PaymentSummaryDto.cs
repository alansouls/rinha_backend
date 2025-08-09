namespace RinhaBackend.Shared.Dtos.Payments.Responses;

public class PaymentSummaryDto
{
    public PaymentServiceSummaryDto Default { get; set; } = new PaymentServiceSummaryDto();
    
    public PaymentServiceSummaryDto Fallback { get; set; } = new PaymentServiceSummaryDto();
}

public class PaymentServiceSummaryDto
{
    public decimal TotalRequests { get; set; }
    
    public decimal TotalAmount { get; set; }
}