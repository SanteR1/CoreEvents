using System.Reflection;
using Events.Domain.Entities;

namespace Events.Tests.Infrastructure
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

        internal static Event CreatePast(
            int hoursInPast,
            string title = "Прошедшее событие",
            string description = "Описание по умолчанию",
            int seats = 10)
        {
            // Получаем ссылку на приватный конструктор
            var constructor = typeof(Event).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(Guid), typeof(string), typeof(DateTime), typeof(DateTime), typeof(int), typeof(string)],
                null);

            if (constructor == null)
            {
                Assert.Fail("Приватный конструктор Event не найден. Проверьте сигнатуру.");
            }

            // Вызываем конструктор напрямую, передавая даты в прошлом
            return (Event)constructor.Invoke(new object[]
            {
                Guid.NewGuid(),
                title,
                DateTime.UtcNow.AddHours(-hoursInPast),
                DateTime.UtcNow.AddHours(-hoursInPast + 2),
                seats,
                description
            });
        }
    }
}
