namespace RinhaBackend.Shared.Domain.Outbox;

public enum OutboxState
{
    Queued,
    Processed,
    Failed,
    ReadyToRetry,
}