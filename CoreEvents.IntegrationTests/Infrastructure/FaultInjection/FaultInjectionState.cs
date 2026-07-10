using System.Collections.Concurrent;

namespace CoreEvents.IntegrationTests.Infrastructure.FaultInjection;

public class FaultInjectionState
{
    // 1. Сбой по событию (оставляем свойством, так как это простой Guid)
    public Guid? TargetEventIdForFailures { get; set; }

    // 2. Сбой по конкретным броням: ТЕПЕРЬ ПРИВАТНЫЙ
    private readonly ConcurrentDictionary<Guid, bool> _targetBookingIdsForFailures = new();

    // 3. Имитация таймаута или "зависшей" базы данных
    public bool SimulateDatabaseTimeout { get; set; }
    public TimeSpan DatabaseDelay { get; set; } = TimeSpan.FromSeconds(30);

    // 4. Имитация конкурентного изменения (Lost Update / Optimistic Concurrency)
    public bool SimulateConcurrencyException { get; set; }

    // 5. Возможность пробросить совершенно кастомную ошибку (например, PostgresException)
    public Exception? CustomExceptionToThrow { get; set; }
    
    /// <summary>
    /// Проверяет, нужно ли имитировать сбой для данной брони
    /// </summary>
    public bool ShouldFailForBooking(Guid bookingId)
    {
        return _targetBookingIdsForFailures.ContainsKey(bookingId);
    }

    /// <summary>
    /// Добавляет бронь в список сбоев (используется в Тестах)
    /// </summary>
    public void AddBookingFailure(Guid bookingId)
    {
        _targetBookingIdsForFailures.TryAdd(bookingId, true);
    }

    /// <summary>
    /// Удаляет бронь из списка сбоев (используется, если нужно отменить сбой прямо в процессе теста)
    /// </summary>
    public void RemoveBookingFailure(Guid bookingId)
    {
        _targetBookingIdsForFailures.TryRemove(bookingId, out _);
    }

    /// <summary>
    /// Сбрасывает все настройки сбоев (вызывается в базовом классе тестов перед каждым [Fact])
    /// </summary>
    public void Reset()
    {
        TargetEventIdForFailures = null;
        _targetBookingIdsForFailures.Clear();
        SimulateDatabaseTimeout = false;
        SimulateConcurrencyException = false;
        CustomExceptionToThrow = null;
    }
}
