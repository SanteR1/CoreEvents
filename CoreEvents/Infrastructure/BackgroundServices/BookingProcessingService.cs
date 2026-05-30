using CoreEvents.Data.DataAccess;
using CoreEvents.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.Infrastructure.BackgroundServices
{
    internal sealed class BookingProcessingService : BackgroundService
    {
        private static readonly int PollingIntervalSeconds  = 10;
        private static readonly int ProcessingDelaySeconds = 2;
        private readonly ILogger<BookingProcessingService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        public BookingProcessingService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingProcessingService> logger)
        {
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
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var pendingBookings = await context.Bookings
                        .Where(x => x.Status == BookingStatus.Pending)
                        .Select(x=> x.Id)
                        .ToListAsync(stoppingToken);

                    var tasks = pendingBookings.Select(bookingId => ProcessBookingAsync(bookingId, stoppingToken));
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
                    _logger.LogError(ex, "Критическая ошибка в главном цикле фоновой обработки.");
                    await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
                }
            }
            _logger.LogInformation("Фоновая служба остановлена.");
        }

        private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Начал обработку брони {id}", bookingId);
            await Task.Delay(TimeSpan.FromSeconds(ProcessingDelaySeconds), stoppingToken);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, stoppingToken);
                if (booking is null || booking.Status != BookingStatus.Pending) return;

                var existEvent = await context.Events.FirstOrDefaultAsync(x => x.Id == booking.EventId, cancellationToken: stoppingToken);
                if (existEvent is null)
                {
                    _logger.LogWarning("Событие не найдено. Отмена брони {Id}.", booking.Id);
                    booking.Reject();
                    await context.SaveChangesAsync(stoppingToken);
                    return;
                }

                booking.Confirm();
                await context.SaveChangesAsync(stoppingToken);

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
                using var rollbackScope = _scopeFactory.CreateScope();
                var context = rollbackScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, stoppingToken);
                if (booking is null || booking.Status != BookingStatus.Pending) return;

                var existEvent = await context.Events.FirstOrDefaultAsync(x => x.Id == booking.EventId, stoppingToken);

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

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(rollbackEx, "Fatal: Не удалось откатить бронь {Id} после первичной ошибки!", bookingId);
            }
        }
    }
}
