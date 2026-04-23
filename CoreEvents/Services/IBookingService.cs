using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;

namespace CoreEvents.Services
{
    public interface IBookingService
    {
        public BookingResponseDto CreateBookingAsync(BookingCreateDto booking);
        public BookingResponseDto GetBookingByIdAsync(Guid bookingId);

    }
}
