namespace CoreEvents.Shared.Contracts.Events;

public enum CancellationReason
{
    UserCancelled = 0,
    EventCancelled = 1,
    AdminCancelled = 2,
    EventRescheduled = 3,
    Timeout = 4
}
