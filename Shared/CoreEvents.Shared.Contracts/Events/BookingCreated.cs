namespace CoreEvents.Shared.Contracts.Events
{
    public record BookingCreated
    {
        public required Guid BookingId { get; init; }
        public required Guid EventId { get; init; }
        public required Guid UserId { get; init; }
        public required int Seats { get; init; }
    }
}
