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
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
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

            await _semaphore.WaitAsync(ct);
            try
            {
                var tryReserve = existEvent.TryReserveSeats();
                if (!tryReserve)
                {
                    throw new NoAvailableSeatsException("No available seats for this event.");
                }

                var booking = new Booking()
                {
                    Id = Guid.NewGuid(),
                    EventId = bookingDto.EventId,
                    Status = BookingStatus.Pending,
                    CreatedAt = DateTime.Now
                };

                _bookingRepository.Add(booking, ct);

                return BookingResponseDto.ToDtoCompiled(booking);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public ValueTask<BookingResponseDto> GetBookingByIdAsync(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var booking = _bookingRepository.GetById(id, ct);
            if (booking == null)
                throw new KeyNotFoundException($"Бронь с ID {id} не найдена.");
            return new ValueTask<BookingResponseDto>(BookingResponseDto.ToDtoCompiled(booking));
        }
    }
}