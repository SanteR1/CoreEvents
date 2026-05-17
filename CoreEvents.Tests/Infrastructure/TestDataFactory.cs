using CoreEvents.Models.Domain;
using static System.Net.WebRequestMethods;

namespace CoreEvents.Tests.Infrastructure
{
    public static class TestDataFactory
    {
        public static EventEntity CreateEvent(
            string title,
            string? description,
            DateTime? startAt = null,
            DateTime? endAt = null,
            int seats = 1) =>
            EventEntity.Create(
                title: title,
                description: description ?? "Test Description",
                startAt: startAt ?? new DateTime(2026, 01, 01, 13, 00, 00),
                endAt: endAt ?? new DateTime(2026, 01, 01, 18, 00, 00),
                totalSeats: seats
            );
    }
}
