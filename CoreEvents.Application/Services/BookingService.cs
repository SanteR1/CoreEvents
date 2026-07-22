using CoreEvents.Application.Configuration;
using CoreEvents.Application.DTOs;
using CoreEvents.Application.Interfaces;
using CoreEvents.Application.Interfaces.Locks;
using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Application.Locks;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.Domain.Exceptions;

namespace CoreEvents.Application.Services
{
    internal sealed class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILockProvider _lockProvider;
        private readonly IUserContext _userContext;
        private readonly BookingSettings _bookingSettings;
        public BookingService(
            IBookingRepository bookingRepository,
            IEventRepository eventRepository,
            ILockProvider lockProvider,
            IUserContext userContext,
            BookingSettings bookingSettings
            )
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _lockProvider = lockProvider;
            _userContext = userContext;
            _bookingSettings = bookingSettings;
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto bookingDto, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var currentUserId = _userContext.UserId.GetValueOrDefault();
            if (currentUserId == Guid.Empty) throw new DomainUnauthorizedAccessException();

            var lockKey = LockKeys.Event(bookingDto.EventId);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            await using var lockScope = await _lockProvider.AcquireLockAsync(lockKey, cts.Token);

            var existEvent = await _eventRepository.GetByIdAsync(bookingDto.EventId, cts.Token);
            if (existEvent is null) throw new DomainNotFoundException(nameof(Event), nameof(bookingDto.EventId), bookingDto.EventId);

            if (existEvent.StartAt <= DateTime.UtcNow) throw new DomainPastEventBookingException(existEvent.Id);

            var bookingCount = await _bookingRepository.GetBookingCountForUserAsync(currentUserId, cts.Token);
            if (bookingCount >= _bookingSettings.MaxBookingsPerUser) throw new DomainActiveBookingLimitExceededException(_bookingSettings.MaxBookingsPerUser);

            if (!existEvent.TryReserveSeats()) throw new DomainNoAvailableSeatsException(existEvent.Id);

            var booking = Booking.Create(bookingDto.EventId, currentUserId);
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
                throw new DomainNotFoundException(nameof(Booking), nameof(id), id);
            return BookingResponseDto.FromEntity(booking);
        }
        public async Task CancelBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var currentUserId = _userContext.UserId.GetValueOrDefault();
            if (currentUserId == Guid.Empty) throw new DomainUnauthorizedAccessException();

            var bookingData = await _bookingRepository.GetByIdAsync(id, ct);
            if (bookingData == null)
                throw new DomainNotFoundException(nameof(Booking), nameof(id), id);

            if (_userContext.Role != RoleName.Admin)
            {
                if (bookingData.UserId != currentUserId) throw new DomainNotBookingOwnerException(id);
            }

            var lockKeyEvent = LockKeys.Event(bookingData.EventId);
            await using var lockScopeEvent = await _lockProvider.AcquireLockAsync(lockKeyEvent, ct);

            var booking = await _bookingRepository.GetByIdAsync(id, ct);
            if (booking == null)
                throw new DomainNotFoundException(nameof(Booking), nameof(id), id);

            if (booking.Status == BookingStatus.Cancelled)
            {
                return;
            }

            var existEvent = await _eventRepository.GetByIdAsync(booking.EventId, ct);
            if (existEvent == null)
                throw new DomainNotFoundException(nameof(Event), nameof(booking.EventId), booking.EventId);

            bool isSuccess = existEvent.ReleaseSeats();

            if (!isSuccess)
            {
                throw new DomainReleaseSeatsException(existEvent.Id, id);
            }

            booking.Cancelled();

            await _bookingRepository.SaveChangesAsync(ct);
            await lockScopeEvent.CompleteAsync(ct);
        }
    }
}