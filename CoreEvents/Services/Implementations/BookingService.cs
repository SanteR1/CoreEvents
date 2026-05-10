using CoreEvents.Data.Queues;
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
        private readonly IBookingQueue _bookingQueue;
        private readonly ILogger<BookingService> _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly object _bookingLock = new();
        public BookingService(IBookingRepository bookingRepository, IBookingQueue bookingQueue, IQueueSource<Guid> queue, ILogger<BookingService> logger, IRepository<EventEntity> eventRepository)
        {
            _bookingRepository = bookingRepository;
            _bookingQueue = bookingQueue;
            _logger = logger;
            _eventRepository = eventRepository;
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto bookingDto, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var existEvent = _eventRepository.GetById(bookingDto.EventId);
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
                _bookingQueue.EnqueueAsync(booking, ct);
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

        public async Task GetBookingForProcessing(CancellationToken ct)
        {
            // TODO => Удалить метод
            //ct.ThrowIfCancellationRequested();
            //var id = Guid.Empty;
            //bool isEntered = false;
            //try
            //{
            //    await _semaphore.WaitAsync(ct);
            //    isEntered = true;

            //    // По замылсу в очереди находятся только со статусом Pending
            //    //if (!_bookingQueue.DequeueAsync(out id)) return;

            //    var booking = _bookingRepository.GetById(id, ct);
            //    if (booking == null) return;

            //    _logger.LogInformation("Начал обработку брони {id}", id);
            //    await Task.Delay(2000, ct);

            //    // ~10% Rejected
            //    var errorProbability = Random.Shared.Next(0, 10);
            //    booking.Status = errorProbability == 0 ? BookingStatus.Rejected : BookingStatus.Confirmed;
            //    booking.ProcessedAt = DateTime.Now;
            //    _logger.LogInformation("Закончил обработку брони {id}", id);

            //    _bookingRepository.Update(booking, ct);
            //}
            //catch (Exception e)
            //{
            //    _logger.LogError(e, "Критический сбой при обработке брони {id}", id);
            //    throw;
            //}
            //finally
            //{
            //    if (isEntered) _semaphore.Release();
            //}
        }
    }
}