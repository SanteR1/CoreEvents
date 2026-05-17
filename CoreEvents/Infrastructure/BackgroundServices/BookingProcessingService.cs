using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using CoreEvents.Services.Interfaces;

namespace CoreEvents.Infrastructure.BackgroundServices
{
    public class BookingProcessingService : BackgroundService
    {
        public int PollingIntervalSeconds { get; set; } = 10;
        public int ProcessingDelaySeconds { get; set; } = 2;

        private readonly ILogger<BookingProcessingService> _logger;
        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
        private readonly IRepository<EventEntity> _eventRepository;
        private readonly IBookingRepository _bookingRepository;


        public BookingProcessingService(
            IRepository<EventEntity> eventRepository,
            IBookingRepository bookingRepository,
            ILogger<BookingProcessingService> logger)
        {
            _eventRepository = eventRepository;
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновая служба обработки броней запущена.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pendingBookings = _bookingRepository.GetPending(stoppingToken).ToList();
                    var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                    await Task.WhenAll(tasks);
                    await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException e) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation(e, "Запрос на остановку службы получен.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Критическая ошибка в фоновой обработке");
                }
            }
            _logger.LogInformation("Фоновая служба остановлена.");
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Начал обработку брони {id}", booking.Id);
            await Task.Delay(TimeSpan.FromSeconds(ProcessingDelaySeconds), stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var existEvent = _eventRepository.GetById(booking.EventId, stoppingToken);
                if (existEvent is null)
                {
                    await HandleRejectionAsync(booking, null, stoppingToken);
                    return;
                }

                await HandleConfirmationAsync(booking, existEvent, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Операция брони с ID {Id} была отменена", booking.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка при обработке бронирования {Id}. Попытка отката...", booking.Id);
                try
                {
                    var existEvent = _eventRepository.GetById(booking.EventId, stoppingToken);
                    await HandleRejectionAsync(booking, existEvent, stoppingToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogCritical(rollbackEx, "Не удалось откатить бронь {Id} после ошибки!", booking.Id);
                }
            }
            finally
            {
                _processingSemaphore.Release();
            }
            _logger.LogInformation("Закончил обработку брони {id}", booking.Id);
        }

        public async Task HandleConfirmationAsync(Booking booking, EventEntity existEvent, CancellationToken ct)
        {
            booking.Confirm();
            _bookingRepository.Update(booking, ct);

            _logger.LogInformation("Бронь {Id} успешно подтверждена для события {EventId}", booking.Id, booking.EventId);
        }

        public async Task HandleRejectionAsync(Booking booking, EventEntity? existEvent, CancellationToken ct)
        {
            booking.Reject();
            _bookingRepository.Update(booking, ct);

            if (existEvent is not null)
            {
                var released = existEvent.ReleaseSeats();
                _eventRepository.Update(existEvent, ct);

                _logger.LogInformation("Бронь {Id} отменена. Места возвращены: {released}. Событие: {EventId}",
                    booking.Id, released, booking.EventId);
            }
            else
            {
                _logger.LogWarning("Бронь {Id} отменена. Событие не найдено, места не возвращены.", booking.Id);
            }
        }
    }
}
