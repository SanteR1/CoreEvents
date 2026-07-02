using CoreEvents.Domain.Entities;

namespace CoreEvents.Tests.Infrastructure
{
    public static class TestEventFactory
    {
        internal static Event Create(
            string title = "Тестовое событие",
            string description = "Описание по умолчанию",
            DateTime? startAt = null,
            DateTime? endAt = null,
            int seats = 10)
        {
            return Event.Create(
                title: title,
                description: description,
                startAt: startAt ?? DateTime.UtcNow.AddDays(1),
                endAt: endAt ?? DateTime.UtcNow.AddDays(1).AddHours(2),
                totalSeats: seats
            );
        }
    }
}
