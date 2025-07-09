namespace RinhaBackend.Shared.Messaging;

public class RabbitMqOptions
{
    public required string HostName { get; set; }
    
    public required int Port { get; set; }
}