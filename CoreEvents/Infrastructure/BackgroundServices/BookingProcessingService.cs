using System.Collections;
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
        private readonly IRepository<Booking> _bookingRepository;


        public BookingProcessingService(IRepository<EventEntity> eventRepository, IRepository<Booking> bookingRepository, IServiceScopeFactory scopeFactory, ILogger<BookingProcessingService> logger)
        {
            _eventRepository = eventRepository;
            _bookingRepository = bookingRepository;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновая служба обработки броней запущена.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    using var scope = _scopeFactory.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                    await bookingService.GetBookingForProcessing(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Операция отменена");
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Критическая ошибка при обработке бронирования");
                }

                await Task.Delay(10000, stoppingToken);
            }
            _logger.LogInformation("Фоновая служба остановлена.");
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Начал обработку брони {id}", booking.Id);
            await Task.Delay(2000, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);

            try
            {
                var existEvent = _eventRepository.GetById(booking.EventId);
                if (existEvent is null)
                {
                    booking.Status = BookingStatus.Rejected;
                    _logger.LogWarning("Событие с ID {booking.EventId} не найдено", booking.EventId);
                }

                _bookingRepository.Update(booking);
                booking.Status = BookingStatus.Confirmed;
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                // TODO:
                // Добавить обработку критической ошибки
            }
            finally
            {
                _processingSemaphore.Release();
            }

        }
    }
}
