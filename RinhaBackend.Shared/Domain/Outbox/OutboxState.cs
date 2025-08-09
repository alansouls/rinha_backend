namespace RinhaBackend.Shared.Domain.Common;

public enum OutboxState
{
    Queued,
    Processed,
    Failed,
    ReadyToRetry,
}