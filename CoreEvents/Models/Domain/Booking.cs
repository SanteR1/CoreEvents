namespace CoreEvents.Models.Domain
{
    public class Booking : IEntity
    {
        public required Guid Id { get; set; }
        public required Guid EventId { get; set; }
        public required BookingStatus Status { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
