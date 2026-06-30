using CoreEvents.Application.Orchestrators;

namespace CoreEvents.Api.BackgroundServices
{
    internal sealed class BookingProcessingService : BackgroundService
    {
        private const int PollingIntervalSeconds = 10;
        private const int ProcessingDelaySeconds = 2;
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

                    var orchestrator = scope.ServiceProvider.GetRequiredService<IBookingOrchestrator>();
                    var idsToProcess = await orchestrator.GetWorkItemsAsync(stoppingToken);
                    
                    var tasks = idsToProcess.Select(id => ProcessSingleBookingSafeAsync(id, stoppingToken));
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

        private async Task ProcessSingleBookingSafeAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(ProcessingDelaySeconds), stoppingToken);

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