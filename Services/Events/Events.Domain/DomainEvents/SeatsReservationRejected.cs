using Events.Domain.Enums;

namespace Events.Domain.DomainEvents;

public sealed record SeatsReservationRejected(
    Guid EventId,
    Guid BookingId,
    int RequestedSeats,
    int AvailableSeats,
    SeatsReservationRejectionReason Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
