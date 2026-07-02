using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreEvents.Application.Orchestrators
{
    internal class BookingOrchestrator : IBookingOrchestrator
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<BookingOrchestrator> _logger;

        private const int ProcessingDelaySeconds = 2;

        public BookingOrchestrator(IBookingRepository bookingRepository,
            IEventRepository eventRepository,
            ILogger<BookingOrchestrator> logger)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<Guid>> GetWorkItemsAsync(CancellationToken cancellationToken)
        {
            return await _bookingRepository.GetPendingAsync(cancellationToken);
        }

        public async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Начал обработку брони {id}", bookingId);

            // Искусственная задержка по условию задания
            await Task.Delay(TimeSpan.FromSeconds(ProcessingDelaySeconds), stoppingToken);

            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking is null || booking.Status != BookingStatus.Pending) return;

                var existEvent = await _eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
                if (existEvent is null)
                {
                    _logger.LogWarning("Событие не найдено. Отмена брони {Id}.", booking.Id);
                    booking.Reject();
                    await _bookingRepository.SaveChangesAsync(stoppingToken);
                    return;
                }

                booking.Confirm();
                await _bookingRepository.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Бронь {Id} успешно подтверждена для события {EventId}", booking.Id, booking.EventId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Операция брони с ID {Id} была отменена", bookingId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка при обработке бронирования {Id}. Попытка отката...", bookingId);
                await RollbackBookingAsync(bookingId, stoppingToken);
            }
            _logger.LogInformation("Закончил обработку брони {id}", bookingId);
        }

        private async Task RollbackBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking is null || booking.Status != BookingStatus.Pending) return;

                var existEvent = await _eventRepository.GetByIdAsync(booking.EventId, stoppingToken);

                booking.Reject();

                if (existEvent is not null)
                {
                    var released = existEvent.ReleaseSeats();
                    _logger.LogInformation("Бронь {Id} отменена (откат). Места возвращены: {released}.", booking.Id, released);
                }
                else
                {
                    _logger.LogWarning("Бронь {Id} отменена (откат). Событие не найдено, места не возвращены.", booking.Id);
                }

                await _bookingRepository.SaveChangesAsync(stoppingToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(rollbackEx, "Fatal: Не удалось откатить бронь {Id} после первичной ошибки!", bookingId);
            }
        }
    }
}
