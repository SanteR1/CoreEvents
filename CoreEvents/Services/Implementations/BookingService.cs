using CoreEvents.Data.Repositories;
using CoreEvents.Middleware;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Interfaces;

namespace CoreEvents.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IRepository<EventEntity> _eventRepository;
        private readonly object _bookingLock = new();
        public BookingService(IBookingRepository bookingRepository, IRepository<EventEntity> eventRepository)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto bookingDto, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var existEvent = _eventRepository.GetById(bookingDto.EventId, ct);
            if (existEvent is null) throw new KeyNotFoundException($"Событие с ID {bookingDto.EventId} не найдено.");

            var booking = new Booking()
            {
                Id = Guid.NewGuid(),
                EventId = bookingDto.EventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            lock (_bookingLock)
            {
                var tryReserve = existEvent.TryReserveSeats();
                if (!tryReserve)
                    throw new NoAvailableSeatsException("No available seats for this event");
                _bookingRepository.Add(booking, ct);
            }

            return new BookingResponseDto(
                booking.Id,
                booking.EventId,
                booking.Status,
                booking.CreatedAt,
                null
            );
        }

        public ValueTask<BookingResponseDto> GetBookingByIdAsync(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var booking = _bookingRepository.GetById(id, ct);
            if (booking == null)
                throw new KeyNotFoundException($"Бронь с ID {id} не найдена.");

            return new ValueTask<BookingResponseDto>(new BookingResponseDto(
                booking.Id,
                booking.EventId,
                booking.Status,
                booking.CreatedAt,
                booking.ProcessedAt));
        }
    }
}