using CoreEvents.Application.DTOs;
using CoreEvents.Application.Interfaces.Locks;
using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Application.Locks;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Exceptions;

namespace CoreEvents.Application.Services
{
    internal sealed class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILockProvider _lockProvider;
        public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, ILockProvider lockProvider)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _lockProvider = lockProvider;
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto bookingDto, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var lockKey = LockKeys.Event(bookingDto.EventId);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            await using var lockScope = await _lockProvider.AcquireLockAsync(lockKey, cts.Token);

            var existEvent = await _eventRepository.GetByIdAsync(bookingDto.EventId, ct);
            if (existEvent is null) throw new DomainNotFoundException(nameof(Event), nameof(bookingDto.EventId), bookingDto.EventId.ToString());

            if (!existEvent.TryReserveSeats()) throw new DomainNoAvailableSeatsException(existEvent.Id);

            var booking = Booking.Create(bookingDto.EventId);
            _bookingRepository.Add(booking);

            await _bookingRepository.SaveChangesAsync(cts.Token);

            await lockScope.CompleteAsync(cts.Token);

            return BookingResponseDto.FromEntity(booking);
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var booking = await _bookingRepository.GetByIdAsync(id, ct);
            if (booking == null)
                throw new DomainNotFoundException(nameof(Booking), nameof(id), id.ToString());
            return BookingResponseDto.FromEntity(booking);
        }
    }
}