namespace Events.Domain.Policies;

public enum ReleaseOutcome
{
    Released,
    AlreadyReleased,
    NotReserved,
    EventMissing,       // резервация Reserved, а события нет
    InvariantViolated   // Release()/ReleaseSeats() вернули false
}
