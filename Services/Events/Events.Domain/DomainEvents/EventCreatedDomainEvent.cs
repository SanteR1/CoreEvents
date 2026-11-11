namespace Events.Domain.DomainEvents;

public sealed record EventCreatedDomainEvent(
    Guid EventId,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    decimal UnitPrice,
    string Currency,
    long PriceVersion,
    int TotalSeats,
    bool IsActive,
    long Version,
    DateTimeOffset OccurredAt) : IDomainEvent;
