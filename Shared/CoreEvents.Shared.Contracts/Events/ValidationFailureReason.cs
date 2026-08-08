namespace CoreEvents.Shared.Contracts.Events;

public enum ValidationFailureReason
{
    EventNotFound = 0,
    EventAlreadyPassed = 1,
    SeatsNotAvailable = 2
}
