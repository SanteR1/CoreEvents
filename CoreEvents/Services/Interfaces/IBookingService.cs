using CoreEvents.Models.DTOs;

namespace CoreEvents.Services.Interfaces
{
    public interface IBookingService
    {
        Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto booking, CancellationToken ct = default);
        Task<BookingResponseDto> GetBookingByIdAsync(Guid booking, CancellationToken ct = default);
    }
}
