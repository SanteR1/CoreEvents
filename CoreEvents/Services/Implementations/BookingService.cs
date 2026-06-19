using CoreEvents.Data.DataAccess;
using CoreEvents.Data.Repositories.Interfaces;
using CoreEvents.Middleware;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Interfaces;

namespace CoreEvents.Services.Implementations
{
    internal sealed class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private static readonly SemaphoreSlim Semaphore = new(1, 1);
        public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto bookingDto, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            await Semaphore.WaitAsync(ct);
            try
            {
                var existEvent = await _eventRepository.GetByIdAsync(bookingDto.EventId, ct);
                if (existEvent is null) throw new NotFoundException($"Событие с ID {bookingDto.EventId} не найдено.");

                if (!existEvent.TryReserveSeats())
                {
                    throw new NoAvailableSeatsException("No available seats for this event.");
                }

                var booking = Booking.Create(bookingDto.EventId);
                _bookingRepository.Add(booking);
                await _bookingRepository.SaveChangesAsync(ct);

                return BookingResponseDto.FromEntity(booking);
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var booking = await _bookingRepository.GetByIdAsync(id, ct);
            if (booking == null)
                throw new NotFoundException($"Бронь с ID {id} не найдена.");
            return BookingResponseDto.FromEntity(booking);
        }
    }
}