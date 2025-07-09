using System.Text.Json.Serialization.Metadata;

namespace RinhaBackend.Shared.Messaging.Interfaces;

public interface IConsumer
{
    IAsyncEnumerable<T> ConsumeAsync<T>(JsonTypeInfo<T> jsonTypeInfo) where T : class;
}