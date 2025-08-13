using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using RinhaBackend.Shared.Domain.Outbox;

namespace RinhaBackend.Shared.Messaging.Interfaces;

public interface IConsumer
{
    IAsyncEnumerable<TypedOutboxMessage<T>> ConsumeAsync<T>(JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken) where T : class;
}