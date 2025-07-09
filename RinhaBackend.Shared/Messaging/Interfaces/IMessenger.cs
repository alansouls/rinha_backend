using System.Text.Json.Serialization.Metadata;

namespace RinhaBackend.Shared.Messaging.Interfaces;

public interface IMessenger
{
   Task SendAsync<T>(T message, JsonTypeInfo<T> jsonTypeInfo) where T : class;
}