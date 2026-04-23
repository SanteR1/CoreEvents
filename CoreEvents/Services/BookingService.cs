using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;

namespace CoreEvents.Services
{
    public class BookingService: IBookingService
    {
        private readonly IRepository<Booking> _repository;
        public BookingService(IRepository<Booking> repository)
        {
            _repository = repository;
        }
        public BookingResponseDto CreateBookingAsync(BookingCreateDto bookingDto)
        {
            var booking = new Booking()
            {
                Id = Guid.NewGuid(),
                Guid = bookingDto.EventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };
            _repository.Add(booking);

            return new BookingResponseDto(
                booking.Id,
                booking.Guid,
                booking.Status,
                booking.CreatedAt,
                null
                );
        }

        public BookingResponseDto GetBookingByIdAsync(Guid bookingId)
        {
            var booking = _repository.GetById(bookingId);
            if (booking == null) throw new KeyNotFoundException($"Бронь с ID {bookingId} не найдена.");

            return new (
                booking.Id,
                booking.Guid,
                booking.Status,
                booking.CreatedAt,
                booking.ProcessedAt
            );
        }
    }
}
