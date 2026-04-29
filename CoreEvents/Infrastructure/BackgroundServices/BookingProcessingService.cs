using CoreEvents.Services.Interfaces;

namespace CoreEvents.Infrastructure.BackgroundServices
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingProcessingService> _logger;

        public BookingProcessingService(IServiceScopeFactory scopeFactory, ILogger<BookingProcessingService> logger)
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
    }
}
