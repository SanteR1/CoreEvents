using Events.Domain.Entities;

namespace Events.Application.Abstractions.Repositories;

public interface ISeatReservationRepository
{
    Task<SeatReservation?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<SeatReservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    void Add(SeatReservation entity);
    void Update(SeatReservation entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
