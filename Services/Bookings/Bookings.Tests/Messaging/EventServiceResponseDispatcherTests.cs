using System.Text.Json;
using AwesomeAssertions;
using Bookings.Application.Commands;
using Bookings.Infrastructure.Messaging;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Exceptions;
using CoreEvents.Shared.Contracts.Registry;
using CoreEvents.Shared.Contracts.Serialization;
using MediatR;
using Moq;

namespace Bookings.Tests.Messaging;

public class EventServiceResponseDispatcherTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IntegrationEventRegistry _registry;
    private readonly EventServiceResponseDispatcher _dispatcher;

    public EventServiceResponseDispatcherTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _registry = new IntegrationEventRegistry();
        _dispatcher = new EventServiceResponseDispatcher(_mediatorMock.Object, _registry);
    }

    [Fact]
    public void SupportedEventTypes_Should_BeEquivalentTo_All_EventTopic_Events()
    {
        var expected = new[]
        {
            _registry.GetName<BookingReservationAccepted>(),
            _registry.GetName<BookingReservationRejected>(),
            _registry.GetName<BookingReservationReleased>(),
            _registry.GetName<EventBookingValidationCompleted>(),
            _registry.GetName<EventBookingCancellationCompleted>(),
            _registry.GetName<EventCreated>(),
            _registry.GetName<EventUpdated>(),
            _registry.GetName<EventCancelled>()
        };

        _dispatcher.SupportedEventTypes.Should().BeEquivalentTo(expected);
        _dispatcher.SupportedEventTypes.Should().HaveCount(8);
    }

    [Fact]
    public void SupportedEventTypes_NegativeTest_ExtraOrMissingType_ShouldFail()
    {
        var actual = _dispatcher.SupportedEventTypes;
        var withMissing = actual.Where(x => !string.Equals(x, _registry.GetName<EventCreated>(), StringComparison.Ordinal)).ToList();
        var withExtra = actual.Concat(["ExtraFakeEvent"]).ToList();

        Action actMissing = () => withMissing.Should().BeEquivalentTo(actual);
        Action actExtra = () => withExtra.Should().BeEquivalentTo(actual);

        actMissing.Should().Throw<Exception>();
        actExtra.Should().Throw<Exception>();
    }

    [Fact]
    public async Task DispatchAsync_BookingReservationAccepted_ShouldSendBookingReservationAcceptedCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var @event = new BookingReservationAccepted
        {
            BookingId = bookingId,
            EventId = eventId,
            Seats = 3,
            UnitPrice = 2500m,
            TotalPrice = 7500m,
            Currency = "KZT",
            DiscountAmount = 0m,
            PriceVersion = 2L,
            ExpiresAt = expiresAt
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingReservationAccepted>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<BookingReservationAcceptedCommand>(c =>
                c.BookingId == bookingId &&
                c.EventId == eventId &&
                c.Seats == 3 &&
                c.UnitPrice == 2500m &&
                c.TotalPrice == 7500m &&
                c.Currency == "KZT" &&
                c.DiscountAmount == 0m &&
                c.PriceVersion == 2L &&
                c.ExpiresAt == expiresAt),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_BookingReservationRejected_ShouldSendRejectBookingCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var @event = new BookingReservationRejected
        {
            BookingId = bookingId,
            EventId = eventId,
            RejectionReason = ValidationFailureReason.SeatsNotAvailable
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingReservationRejected>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<RejectBookingCommand>(c =>
                c.BookingId == bookingId &&
                c.Reason == ValidationFailureReason.SeatsNotAvailable),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_BookingReservationReleased_ShouldSendApplyBookingCancellationCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var @event = new BookingReservationReleased
        {
            BookingId = bookingId,
            EventId = eventId,
            Seats = 2,
            ReleasedAt = DateTimeOffset.UtcNow
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingReservationReleased>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<ApplyBookingCancellationCommand>(c =>
                c.BookingId == bookingId),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_EventCreated_ShouldSendSyncEventCreatedCommand()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var startAt = DateTime.UtcNow.AddDays(1);
        var endAt = DateTime.UtcNow.AddDays(2);
        var createdAt = DateTimeOffset.UtcNow;
        var @event = new EventCreated
        {
            EventId = eventId,
            Title = "Архитектурный митап",
            StartAt = startAt,
            EndAt = endAt,
            UnitPrice = 5000m,
            Currency = "KZT",
            PriceVersion = 1L,
            TotalSeats = 100,
            IsActive = true,
            Version = 1L,
            CreatedAt = createdAt
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<EventCreated>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<SyncEventCreatedCommand>(c =>
                c.EventId == eventId &&
                c.Title == "Архитектурный митап" &&
                c.StartAt == startAt &&
                c.EndAt == endAt &&
                c.UnitPrice == 5000m &&
                c.Currency == "KZT" &&
                c.PriceVersion == 1L &&
                c.TotalSeats == 100 &&
                c.IsActive &&
                c.Version == 1L &&
                c.CreatedAt == createdAt),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_EventUpdated_ShouldSendSyncEventUpdatedCommand()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var startAt = DateTime.UtcNow.AddDays(3);
        var endAt = DateTime.UtcNow.AddDays(4);
        var updatedAt = DateTimeOffset.UtcNow;
        var @event = new EventUpdated
        {
            EventId = eventId,
            Title = "Обновленный митап",
            Description = "Новое описание",
            StartAt = startAt,
            EndAt = endAt,
            UnitPrice = 7000m,
            Currency = "USD",
            PriceVersion = 2L,
            TotalSeats = 150,
            IsActive = true,
            Version = 3L,
            UpdatedAt = updatedAt
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<EventUpdated>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<SyncEventUpdatedCommand>(c =>
                c.EventId == eventId &&
                c.Title == "Обновленный митап" &&
                c.StartAt == startAt &&
                c.EndAt == endAt &&
                c.UnitPrice == 7000m &&
                c.Currency == "USD" &&
                c.PriceVersion == 2L &&
                c.TotalSeats == 150 &&
                c.IsActive &&
                c.Version == 3L &&
                c.UpdatedAt == updatedAt),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_EventCancelled_ShouldSendSyncEventCancelledCommand()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancelledAt = DateTimeOffset.UtcNow;
        var @event = new EventCancelled
        {
            EventId = eventId,
            Reason = "Отменено организатором",
            Version = 4L,
            CancelledAt = cancelledAt
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<EventCancelled>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<SyncEventCancelledCommand>(c =>
                c.EventId == eventId &&
                c.Reason == "Отменено организатором" &&
                c.Version == 4L &&
                c.CancelledAt == cancelledAt),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_EventBookingValidationCompleted_WhenCanBeBookedTrue_ShouldSendConfirmBookingCommand()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var @event = new EventBookingValidationCompleted
        {
            EventId = eventId,
            BookingId = bookingId,
            CanBeBooked = true,
            FailureReason = null
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<EventBookingValidationCompleted>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<ConfirmBookingCommand>(c =>
                c.EventId == eventId &&
                c.BookingId == bookingId),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_EventBookingValidationCompleted_WhenCanBeBookedFalseWithReason_ShouldSendRejectBookingCommand()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var @event = new EventBookingValidationCompleted
        {
            EventId = eventId,
            BookingId = bookingId,
            CanBeBooked = false,
            FailureReason = ValidationFailureReason.EventAlreadyPassed
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<EventBookingValidationCompleted>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<RejectBookingCommand>(c =>
                c.BookingId == bookingId &&
                c.Reason == ValidationFailureReason.EventAlreadyPassed),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_EventBookingValidationCompleted_WhenCanBeBookedFalseWithNullReason_ShouldSendRejectBookingCommandWithDefaultReason()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var @event = new EventBookingValidationCompleted
        {
            EventId = eventId,
            BookingId = bookingId,
            CanBeBooked = false,
            FailureReason = null
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<EventBookingValidationCompleted>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<RejectBookingCommand>(c =>
                c.BookingId == bookingId &&
                c.Reason == default(ValidationFailureReason)),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_EventBookingCancellationCompleted_ShouldSendApplyBookingCancellationCommand()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var @event = new EventBookingCancellationCompleted
        {
            EventId = eventId,
            BookingId = bookingId,
            SeatsReleased = true
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<EventBookingCancellationCompleted>(), payload, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<ApplyBookingCancellationCommand>(c =>
                c.BookingId == bookingId),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_UnknownEvent_ShouldThrowUnknownEventTypeException()
    {
        // Arrange
        const string unknownEventType = "SomeUnknownEventType";

        // Act
        Func<Task> act = async () => await _dispatcher.DispatchAsync(unknownEventType, "{}", CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<UnknownEventTypeException>();
        exception.Which.EventType.Should().Be(unknownEventType);
    }

    [Fact]
    public async Task DispatchAsync_NullJsonPayload_ShouldThrowJsonException()
    {
        // Act
        Func<Task> act = async () => await _dispatcher.DispatchAsync(_registry.GetName<BookingReservationAccepted>(), "null", CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<JsonException>();
        exception.WithMessage($"*Invalid {nameof(BookingReservationAccepted)} payload*");
    }

    [Fact]
    public async Task DispatchAsync_MalformedJsonPayload_ShouldThrowJsonException()
    {
        // Act
        Func<Task> act = async () => await _dispatcher.DispatchAsync(_registry.GetName<BookingReservationAccepted>(), "{not-valid-json", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }
}
