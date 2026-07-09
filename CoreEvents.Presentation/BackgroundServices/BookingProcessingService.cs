using CoreEvents.Application.Orchestrators;

namespace CoreEvents.Presentation.BackgroundServices
{
    internal sealed class BookingProcessingService : BackgroundService
    {
        private readonly int _pollingIntervalMilliseconds;
        private readonly ILogger<BookingProcessingService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public BookingProcessingService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingProcessingService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _pollingIntervalMilliseconds = configuration.GetValue<int>("BackgroundServices:BookingInterval", 10_000);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_pollingIntervalMilliseconds));
            _logger.LogInformation("Фоновая служба обработки броней запущена.");

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var orchestrator = scope.ServiceProvider.GetRequiredService<IBookingOrchestrator>();
                    var idsToProcess = await orchestrator.GetWorkItemsAsync(stoppingToken);
                    
                    var tasks = idsToProcess.Select(id => ProcessSingleBookingSafeAsync(id, stoppingToken));
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException e) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation(e, "Запрос на остановку службы получен.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Критическая ошибка в главном цикле фоновой обработки.");
                }
            }
            _logger.LogInformation("Фоновая служба остановлена.");
        }

        private async Task ProcessSingleBookingSafeAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var useCase = scope.ServiceProvider.GetRequiredService<IBookingOrchestrator>();

                await useCase.ProcessBookingAsync(bookingId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ошибка инфраструктуры при обработке брони {Id}", bookingId);
            }
        }
    }
}