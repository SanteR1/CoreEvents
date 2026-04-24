using CoreEvents.Data.Queues;
using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Interfaces;

namespace CoreEvents.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly IRepository<Booking> _repository;
        private readonly IEventService _eventService;
        private readonly IQueueSource<Guid> _queue;
        private readonly ILogger<BookingService> _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        public BookingService(IRepository<Booking> repository, IQueueSource<Guid> queue, IEventService eventService, ILogger<BookingService> logger)
        {
            _repository = repository;
            _queue = queue;
            _eventService = eventService;
            _logger = logger;
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto bookingDto, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var existEvent = await _eventService.GetEventById(bookingDto.EventId);
            
                if (existEvent is null)
                    throw new KeyNotFoundException($"Событие с ID {bookingDto.EventId} не найдено.");
            

            var booking = new Booking()
            {
                Id = Guid.NewGuid(),
                Guid = bookingDto.EventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };
            _repository.Add(booking, ct);
            _queue.Enqueue(booking.Id);

            return new BookingResponseDto(
                booking.Id,
                booking.Guid,
                booking.Status,
                booking.CreatedAt,
                null
            );
        }

        public ValueTask<BookingResponseDto> GetBookingByIdAsync(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var booking = _repository.GetById(id, ct);
            if (booking == null)
                throw new KeyNotFoundException($"Бронь с ID {id} не найдена.");


            return new ValueTask<BookingResponseDto>(new BookingResponseDto(
                booking.Id,
                booking.Guid,
                booking.Status,
                booking.CreatedAt,
                booking.ProcessedAt));
        }

        public async Task GetBookingForProcessing(CancellationToken ct)
        {
            var id = Guid.Empty;
            try
            {
                ct.ThrowIfCancellationRequested();
                await _semaphore.WaitAsync(ct);

                if (!_queue.TryDequeue(out id)) return;

                var booking = _repository.GetById(id, ct);
                if (booking == null) return;

                _logger.LogInformation("Начал обработку брони {id}", id);
                await Task.Delay(2000, ct);

                // ~10% Rejected
                var errorProbability = Random.Shared.Next(0, 10);
                booking.Status = errorProbability == 0 ? BookingStatus.Rejected : BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.Now;
                _logger.LogInformation("Закончил обработку брони {id}", id);

                _repository.Update(booking, ct);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Критический сбой при обработке брони {id}", id);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}