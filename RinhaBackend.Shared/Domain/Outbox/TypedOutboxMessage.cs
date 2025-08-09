namespace RinhaBackend.Shared.Domain.Outbox;

public class TypedOutboxMessage<T>
{
    public Guid Id { get; set; }
    
    public required T Payload { get; set; }
}