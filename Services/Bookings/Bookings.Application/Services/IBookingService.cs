using Bookings.Application.DTOs;

namespace Bookings.Application.Services
{
    public interface IBookingService
    {
        Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto booking, CancellationToken ct = default);
        Task<BookingResponseDto> GetBookingByIdAsync(Guid booking, CancellationToken ct = default);
        Task CancelBookingByIdAsync(Guid booking, CancellationToken ct = default);
    }
}
