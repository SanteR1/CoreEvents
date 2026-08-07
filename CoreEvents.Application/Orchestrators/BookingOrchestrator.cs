using CoreEvents.Application.Interfaces.Locks;
using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Application.Locks;
using CoreEvents.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreEvents.Application.Orchestrators
{
    internal class BookingOrchestrator : IBookingOrchestrator
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingOrchestrator> _logger;
        private readonly ILockProvider _lockProvider;

        private const int ProcessingDelaySeconds = 2;

        public BookingOrchestrator(IServiceScopeFactory scopeFactory,
            ILogger<BookingOrchestrator> logger,
            ILockProvider lockProvider)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _lockProvider = lockProvider;
        }

        public async Task<IReadOnlyCollection<Guid>> GetWorkItemsAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            return await bookingRepository.GetPendingAsync(cancellationToken);
        }

        public async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            var lockKey = LockKeys.Booking(bookingId);

            await using var lockScope = await _lockProvider.TryAcquireLockAsync(lockKey, stoppingToken);

            if (lockScope == null)
            {
                _logger.LogDebug("Бронь {id} уже в обработке другим инстансом. Пропуск.", bookingId);
                return;
            }

            _logger.LogInformation("Начал обработку брони {id} (блокировка получена)", bookingId);

            try
            {
                // Искусственная задержка по условию задания увеличивает время интеграционных тестов
                // await Task.Delay(TimeSpan.FromSeconds(ProcessingDelaySeconds), stoppingToken);

                await using var mainScope = _scopeFactory.CreateAsyncScope();
                var bookingRepository = mainScope.ServiceProvider.GetRequiredService<IBookingRepository>();

                var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking is null || booking.Status != BookingStatus.Pending) return;

                var eventRepository = mainScope.ServiceProvider.GetRequiredService<IEventRepository>();
                var existEvent = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
                if (existEvent is null)
                {
                    _logger.LogWarning("Событие не найдено. Отмена брони {id}.", booking.Id);
                    booking.Reject();
                    await bookingRepository.SaveChangesAsync(stoppingToken);

                    await lockScope.CompleteAsync(stoppingToken);
                    return;
                }

                booking.Confirm();

                await bookingRepository.SaveChangesAsync(stoppingToken);

                await lockScope.CompleteAsync(stoppingToken);

                _logger.LogInformation("Бронь {id} успешно подтверждена для события {EventId}", booking.Id, booking.EventId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Операция брони с ID {id} была отменена", bookingId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка при обработке бронирования {id}. Попытка отката...", bookingId);
                await RollbackBookingAsync(bookingId, lockScope, stoppingToken);
            }
            _logger.LogInformation("Закончил обработку брони {id}", bookingId);
        }

        private async Task RollbackBookingAsync(Guid bookingId, ILockScope lockScope, CancellationToken stoppingToken)
        {
            try
            {
                await using var rollbackScope = _scopeFactory.CreateAsyncScope();
                var bookingRepository = rollbackScope.ServiceProvider.GetRequiredService<IBookingRepository>();

                var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking is null || booking.Status != BookingStatus.Pending) return;

                var eventLockProvider = rollbackScope.ServiceProvider.GetRequiredService<ILockProvider>();

                var lockKey = LockKeys.Event(booking.EventId);

                await using var eventLock = await eventLockProvider.TryAcquireLockAsync(lockKey, stoppingToken);

                if (eventLock == null)
                {
                    _logger.LogWarning("Не удалось захватить блокировку для события {EventId}. Откат брони {id} отложен.", booking.EventId, bookingId);
                    return;
                }

                var eventRepository = rollbackScope.ServiceProvider.GetRequiredService<IEventRepository>();
                var existEvent = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);

                booking.Reject();

                if (existEvent is not null)
                {
                    var released = existEvent.ReleaseSeats();

                    _logger.LogInformation("Бронь {id} отменена (откат). Места возвращены: {released}.", booking.Id, released);
                }
                else
                {
                    _logger.LogWarning("Бронь {id} отменена (откат). Событие не найдено, места не возвращены.", booking.Id);
                }

                await bookingRepository.SaveChangesAsync(stoppingToken);

                await eventLock.CompleteAsync(stoppingToken);
                await lockScope.CompleteAsync(stoppingToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(rollbackEx, "Fatal: Не удалось откатить бронь {id} после первичной ошибки!", bookingId);
            }
        }
    }
}
