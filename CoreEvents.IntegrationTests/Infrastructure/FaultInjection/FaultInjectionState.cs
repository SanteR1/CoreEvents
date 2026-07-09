using System.Collections.Concurrent;


namespace CoreEvents.IntegrationTests.Infrastructure.FaultInjection
{
    // 1. Создаем легкий класс состояния
    public class FaultInjectionState
    {
        // 1. Сбой по событию (наш шторм из 90 броней)
        public Guid? TargetEventIdForFailures { get; set; }

        // 2. Сбой по конкретным броням (полезно для точечных тестов на 1-2 брони)
        public ConcurrentDictionary<Guid, bool> TargetBookingIdsForFailures { get; } = new();

        // 3. Имитация таймаута или "зависшей" базы данных
        public bool SimulateDatabaseTimeout { get; set; }
        public TimeSpan DatabaseDelay { get; set; } = TimeSpan.FromSeconds(30);

        // 4. Имитация конкурентного изменения (Lost Update / Optimistic Concurrency)
        public bool SimulateConcurrencyException { get; set; }

        // 5. Возможность пробросить совершенно кастомную ошибку (например, PostgresException)
        public Exception? CustomExceptionToThrow { get; set; }

        // Вызывайте этот метод в конструкторе (или IAsyncLifetime.InitializeAsync) вашего тестового класса!
        public void Reset()
        {
            TargetEventIdForFailures = null;
            TargetBookingIdsForFailures.Clear();
            SimulateDatabaseTimeout = false;
            SimulateConcurrencyException = false;
            CustomExceptionToThrow = null;
        }
    }
}
