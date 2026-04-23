namespace CoreEvents.Models.Domain
{
    public class Booking : IEntity
    {
        public Guid Id { get; set; }
        public Guid Guid { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
