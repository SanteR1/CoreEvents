using CoreEvents.Application.DTOs;

namespace CoreEvents.Application.Services
{
    public interface IBookingService
    {
        Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto booking, CancellationToken ct = default);
        Task<BookingResponseDto> GetBookingByIdAsync(Guid booking, CancellationToken ct = default);
    }
}
