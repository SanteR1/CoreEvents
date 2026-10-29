using Bookings.Domain.Enums;

namespace Bookings.Application.DTOs;

public record BookingFilter : PagedFilter
{
    public BookingStatus? Status { get; init; }
    public Guid? UserId { get; init; }
}
