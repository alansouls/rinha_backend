namespace RinhaBackend.Shared.Domain.Inbox;

public class InboxMessage
{
    public Guid Id { get; set; }
    
    public Guid OutboxMessageId { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
    
    public int Retries { get; set; }
    
    public InboxState State { get; set; }
}