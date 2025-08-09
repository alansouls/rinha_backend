namespace RinhaBackend.Shared.Domain.Inbox;

public enum InboxState
{
    Running,
    Succeeded,
    Failed,
    ReadyToRetry,
}