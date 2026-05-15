using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;

namespace CoreEvents.Infrastructure.BackgroundServices
{
    public class BookingProcessingService : BackgroundService, IBookingProcessingService
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

        public async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Начал обработку брони {id}", booking.Id);
            await Task.Delay(TimeSpan.FromSeconds(ProcessingDelaySeconds), stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var existEvent = _eventRepository.GetById(booking.EventId, stoppingToken);
                if (existEvent is null)
                {
                    booking.Reject();
                    _bookingRepository.Update(booking, stoppingToken);
                    _logger.LogWarning("Событие {EventId} не найдено. Бронь {Id} отменена.", booking.EventId, booking.Id);
                    return;
                }

                booking.Confirm();
                _bookingRepository.Update(booking, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Операция брони с ID {Id} была отменена", booking.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Критическая ошибка при обработке бронирования с ID {Id}", booking.Id);
                try
                {
                    var existEvent = _eventRepository.GetById(booking.EventId, stoppingToken);
                    if (existEvent is not null)
                    {
                        booking.Reject();
                        var releaseSeats = existEvent.ReleaseSeats();
                        _eventRepository.Update(existEvent, stoppingToken);
                        _bookingRepository.Update(booking, stoppingToken);
                        _logger.LogInformation("Результат попытки отмены забронированных мест: {releaseSeats}", releaseSeats);
                    }
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
    }
}
