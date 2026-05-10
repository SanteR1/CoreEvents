using System.Collections;
using CoreEvents.Data.Queues;
using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using CoreEvents.Services.Interfaces;

namespace CoreEvents.Infrastructure.BackgroundServices
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingProcessingService> _logger;
        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
        private readonly IRepository<EventEntity> _eventRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IBookingQueue _bookingQueue;


        public BookingProcessingService(
            IRepository<EventEntity> eventRepository,
            IBookingRepository bookingRepository,
            IServiceScopeFactory scopeFactory,
            ILogger<BookingProcessingService> logger,
            IBookingQueue bookingQueue)
        {
            _eventRepository = eventRepository;
            _bookingRepository = bookingRepository;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _bookingQueue = bookingQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновая служба обработки броней запущена.");
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 10
                };

                await Parallel.ForEachAsync(
                    _bookingQueue.DequeueAsync(stoppingToken),
                    options,
                    async (booking, token) =>
                    {
                        await ProcessBookingAsync(booking, token);
                    });
            }
            catch (OperationCanceledException e) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(e, "Отмена операции в фоновой обработке");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка в фоновой обработке");
            }
            _logger.LogInformation("Фоновая служба остановлена.");
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            EventEntity? existEvent = null;
            _logger.LogInformation("Начал обработку брони {id}", booking.Id);
            await Task.Delay(2000, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                existEvent = _eventRepository.GetById(booking.EventId);
                if (existEvent is null)
                {
                    _logger.LogWarning("Событие с ID {EventId} не найдено", booking.EventId);
                    booking.Status = BookingStatus.Rejected;
                    booking.ProcessedAt = DateTime.Now;
                    _bookingRepository.Update(booking, stoppingToken);
                    return;
                }

                booking.Status = BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.Now;

                _bookingRepository.Update(booking, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Операция брони с ID {Id} была отменена", booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Критическая ошибка при обработке бронирования с ID {Id}", booking.Id);
                try
                {
                    existEvent = _eventRepository.GetById(booking.EventId);
                    if (existEvent is not null)
                    {
                        var releaseSeats = existEvent.ReleaseSeats();
                        booking.Status = BookingStatus.Rejected;
                        booking.ProcessedAt = DateTime.Now;
                        _bookingRepository.Update(booking, stoppingToken);
                        _logger.LogInformation(ex, "Забронированные места были восстановлены: {releaseSeats}", releaseSeats);
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
