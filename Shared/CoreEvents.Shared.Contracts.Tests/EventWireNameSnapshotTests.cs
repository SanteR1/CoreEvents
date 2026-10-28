namespace CoreEvents.Shared.Contracts.Tests;

using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Registry;
using Xunit;

public class EventWireNameSnapshotTests
{
    private static readonly string[] ExpectedWireNames =
    [
        "booking.cancellation-requested.v1",
        "booking.cancelled.v1",
        "booking.confirmed.v1",
        "booking.created.v1",
        "booking.rejected.v1",
        "booking.reservation-accepted.v1",
        "booking.reservation-rejected.v1",
        "booking.reservation-release-requested.v1",
        "booking.reservation-released.v1",
        "booking.reservation-requested.v1",
        "event.booking-cancellation-completed.v1",
        "event.booking-validation-completed.v1",
        "event.cancelled.v1",
        "event.created.v1",
        "event.updated.v1"
    ];

    [Fact]
    public void EventWireNames_Should_Match_Snapshot_Ordinal_Sorted()
    {
        var registry = new IntegrationEventRegistry();
        var types = ContractTypes.AllConcreteIntegrationEvents();

        var actualWireNames = types
            .Select(t => registry.GetName(t))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Проверка уникальности
        actualWireNames.Should().OnlyHaveUniqueItems("Все wire-имена событий должны быть уникальными");

        // Сравнение со снимком (строго по порядку)
        actualWireNames.Should().Equal(ExpectedWireNames);
    }
}
