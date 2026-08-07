namespace Bookings.Application.Abstractions
{
    public interface IOutboxService
    {
        // Метод просто подготавливает сообщение к отправке (добавляет в DbSet)
        /// <param name="integrationEvent">Событие/результат для публикации.</param>
        /// <param name="partitionKey">
        /// Ключ партиционирования Kafka. Выбирается вызывающей стороной осознанно:
        /// например, EventId — если важен строгий порядок в рамках сущности (резервация мест),
        /// BookingId — если сущности независимы и важнее параллелизм (результат резервации).
        /// </param>
        void Publish<T>(T integrationEvent, string partitionKey);
    }
}
