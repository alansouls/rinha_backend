using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using RinhaBackend.Shared.Domain.Common;

namespace RinhaBackend.Shared.Domain.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; }
    
    public required string Type { get; set; }
    
    public required string Payload { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
    
    public int Retries { get; set; }
    
    public OutboxState State { get; set; }

    public TypedOutboxMessage<T> ToTyped<T>(JsonTypeInfo<T> jsonTypeInfo)
    {
        return new TypedOutboxMessage<T>
        {
            Id = Id,
            Payload = JsonSerializer.Deserialize(Payload, jsonTypeInfo)!
        };
    }
}