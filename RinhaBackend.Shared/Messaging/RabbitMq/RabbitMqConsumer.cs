using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RinhaBackend.Shared.Messaging.Interfaces;

namespace RinhaBackend.Shared.Messaging.RabbitMq;

internal class RabbitMqConsumer : IConsumer
{
    private readonly RabbitMqChannelProvider _channelProvider;

    public RabbitMqConsumer(RabbitMqChannelProvider channelProvider)
    {
        _channelProvider = channelProvider;
    }

    public async IAsyncEnumerable<T> ConsumeAsync<T>(JsonTypeInfo<T> jsonTypeInfo) where T : class
    {
        var channel = await _channelProvider.GetChannel(typeof(T).Name);

        var resultTaskCompletionSource = new TaskCompletionSource<T>();

        var consumer = new AsyncEventingBasicConsumer(channel);
        
        AsyncEventHandler<BasicDeliverEventArgs> handler = (ch, args) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize(args.Body.Span, jsonTypeInfo)
                              ?? throw new Exception("Deserialized message is null");
                
                resultTaskCompletionSource.SetResult(message);
            }
            catch (Exception e)
            {
                resultTaskCompletionSource.SetException(e);
            }

            return Task.CompletedTask;
        };
        consumer.ReceivedAsync += handler;
        
        await channel.BasicConsumeAsync(typeof(T).Name, autoAck: true, consumer);

        while (true)
        {
            yield return await resultTaskCompletionSource.Task;
            resultTaskCompletionSource = new TaskCompletionSource<T>();
        }
    }
}