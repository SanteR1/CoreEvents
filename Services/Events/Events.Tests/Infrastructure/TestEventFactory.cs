using System.Reflection;
using Events.Domain.Entities;

namespace Events.Tests.Infrastructure;

public static class TestEventFactory
{
    internal static Event Create(
        string title = "Тестовое событие",
        string description = "Описание по умолчанию",
        DateTime? startAt = null,
        DateTime? endAt = null,
        int seats = 10,
        decimal price = 0.00m,
        string currency = "KZT")
    {
        return Event.Create(
            title: title,
            description: description,
            startAt: startAt ?? DateTime.UtcNow.AddDays(1),
            endAt: endAt ?? DateTime.UtcNow.AddDays(1).AddHours(2),
            totalSeats: seats,
            price: price,
            currency: currency
        );
    }

    internal static Event CreatePast(
        int hoursInPast,
        string title = "Прошедшее событие",
        string description = "Описание по умолчанию",
        int seats = 10)
    {
        var constructor = typeof(Event).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(Guid), typeof(string), typeof(DateTime), typeof(DateTime), typeof(int), typeof(string), typeof(decimal), typeof(string), typeof(long), typeof(bool), typeof(long)],
            null);

        if (constructor != null)
        {
            return (Event)constructor.Invoke([
                Guid.NewGuid(),
                title,
                DateTime.UtcNow.AddHours(-hoursInPast),
                DateTime.UtcNow.AddHours(-hoursInPast + 2),
                seats,
                description,
                100m,
                "KZT",
                1L,
                true,
                1L
            ]);
        }

        // Fallback using Activator
        var instance = (Event)Activator.CreateInstance(typeof(Event), nonPublic: true)!;
        var startField = typeof(Event).GetProperty(nameof(Event.StartAt))!;
        var endField = typeof(Event).GetProperty(nameof(Event.EndAt))!;
        var titleProp = typeof(Event).GetProperty(nameof(Event.Title))!;
        var seatsProp = typeof(Event).GetProperty(nameof(Event.TotalSeats))!;
        var availProp = typeof(Event).GetProperty(nameof(Event.AvailableSeats))!;

        startField.SetValue(instance, DateTime.UtcNow.AddHours(-hoursInPast));
        endField.SetValue(instance, DateTime.UtcNow.AddHours(-hoursInPast + 2));
        titleProp.SetValue(instance, title);
        seatsProp.SetValue(instance, seats);
        availProp.SetValue(instance, seats);

        return instance;
    }
}
