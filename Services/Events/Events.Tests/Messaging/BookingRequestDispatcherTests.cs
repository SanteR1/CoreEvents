using System.Text.Json;
using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Exceptions;
using CoreEvents.Shared.Contracts.Registry;
using CoreEvents.Shared.Contracts.Serialization;
using Events.Application.Abstractions.Messaging;
using Events.Application.Commands;
using Events.Infrastructure.Messaging;
using MediatR;
using Moq;

namespace Events.Tests.Messaging;

public sealed class BookingRequestDispatcherTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IntegrationEventRegistry _registry;
    private readonly BookingRequestDispatcher _dispatcher;

    public BookingRequestDispatcherTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _registry = new IntegrationEventRegistry();
        _dispatcher = new BookingRequestDispatcher(_mediatorMock.Object, _registry);
    }

    [Fact]
    public void SupportedEventTypes_Should_BeEquivalentTo_All_Registered_Booking_Events()
    {
        var expected = new[]
        {
            _registry.GetName<BookingReservationRequested>(),
            _registry.GetName<BookingReservationReleaseRequested>(),
            _registry.GetName<BookingCancelled>(),
            _registry.GetName<BookingRejected>(),
            _registry.GetName<BookingCreated>(),
            _registry.GetName<BookingCancellationRequested>(),
            _registry.GetName<BookingConfirmed>()
        };

        _dispatcher.SupportedEventTypes.Should().BeEquivalentTo(expected);
        _dispatcher.SupportedEventTypes.Should().HaveCount(7);
    }

    [Fact]
    public void SupportedEventTypes_NegativeTest_ExtraOrMissingType_ShouldFail()
    {
        var actual = _dispatcher.SupportedEventTypes;
        var withMissing = actual.Where(x => !string.Equals(x, _registry.GetName<BookingCancelled>(), StringComparison.Ordinal)).ToList();
        var withExtra = actual.Concat(["ExtraFakeEvent"]).ToList();

        Action actMissing = () => withMissing.Should().BeEquivalentTo(actual);
        Action actExtra = () => withExtra.Should().BeEquivalentTo(actual);

        actMissing.Should().Throw<Exception>();
        actExtra.Should().Throw<Exception>();
    }

    [Fact]
    public async Task DispatchAsync_BookingReservationRequested_ShouldSendBookingReservationRequestCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var envelope = CreateEnvelope(_registry.GetName<BookingReservationRequested>());
        var @event = new BookingReservationRequested
        {
            BookingId = bookingId,
            EventId = eventId,
            UserId = userId,
            Seats = 2
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingReservationRequested>(), payload, envelope, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<BookingReservationRequestCommand>(c =>
                c.BookingId == bookingId &&
                c.EventId == eventId &&
                c.UserId == userId &&
                c.Seats == 2 &&
                c.Inbox == envelope),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_BookingReservationReleaseRequested_ShouldSendBookingReservationReleaseRequestCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var envelope = CreateEnvelope(_registry.GetName<BookingReservationReleaseRequested>());
        var @event = new BookingReservationReleaseRequested
        {
            BookingId = bookingId,
            EventId = eventId,
            Seats = 4,
            Reason = CancellationReason.UserCancelled
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingReservationReleaseRequested>(), payload, envelope, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<BookingReservationReleaseRequestCommand>(c =>
                c.BookingId == bookingId &&
                c.EventId == eventId &&
                c.Seats == 4 &&
                c.Reason == CancellationReason.UserCancelled &&
                c.Inbox == envelope),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_BookingCancelled_ShouldSendSyncBookingCancelledCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var envelope = CreateEnvelope(_registry.GetName<BookingCancelled>());
        var @event = new BookingCancelled
        {
            BookingId = bookingId,
            EventId = eventId,
            UserId = userId,
            Reason = CancellationReason.UserCancelled,
            CancelledAt = DateTimeOffset.UtcNow
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingCancelled>(), payload, envelope, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<SyncBookingCancelledCommand>(c =>
                c.BookingId == bookingId &&
                c.EventId == eventId &&
                c.Inbox == envelope),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_BookingRejected_ShouldSendSyncBookingCancelledCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var envelope = CreateEnvelope(_registry.GetName<BookingRejected>());
        var @event = new BookingRejected
        {
            BookingId = bookingId,
            EventId = eventId,
            UserId = userId,
            Reason = ValidationFailureReason.SeatsNotAvailable,
            RejectedAt = DateTimeOffset.UtcNow
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingRejected>(), payload, envelope, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<SyncBookingCancelledCommand>(c =>
                c.BookingId == bookingId &&
                c.EventId == eventId &&
                c.Inbox == envelope),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_BookingCreated_ShouldSendValidateBookingCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var envelope = CreateEnvelope(_registry.GetName<BookingCreated>());
        var @event = new BookingCreated
        {
            BookingId = bookingId,
            EventId = eventId,
            UserId = userId,
            Seats = 5
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingCreated>(), payload, envelope, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<ValidateBookingCommand>(c =>
                c.BookingId == bookingId &&
                c.EventId == eventId &&
                c.Seats == 5 &&
                c.Inbox == envelope),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_BookingCancellationRequested_ShouldSendReleaseSeatsCommand()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var envelope = CreateEnvelope(_registry.GetName<BookingCancellationRequested>());
        var @event = new BookingCancellationRequested
        {
            BookingId = bookingId,
            EventId = eventId,
            UserId = userId,
            Seats = 3,
            Reason = CancellationReason.UserCancelled,
            CancelledAt = DateTimeOffset.UtcNow
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingCancellationRequested>(), payload, envelope, cts.Token);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<ReleaseSeatsCommand>(c =>
                c.BookingId == bookingId &&
                c.EventId == eventId &&
                c.Seats == 3 &&
                c.Inbox == envelope),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_BookingConfirmed_ShouldSendProcessNoOpInboxCommand()
    {
        // Arrange
        var envelope = CreateEnvelope(_registry.GetName<BookingConfirmed>());
        var @event = new BookingConfirmed
        {
            BookingId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Seats = 3
        };
        var payload = JsonSerializer.Serialize(@event, IntegrationEventJsonOptions.Default);
        using var cts = new CancellationTokenSource();

        // Act
        await _dispatcher.DispatchAsync(_registry.GetName<BookingConfirmed>(), payload, envelope, cts.Token);

        // Assert: Осознанный no-op регистрируется в Inbox через ProcessNoOpInboxCommand
        _mediatorMock.Verify(m => m.Send(
            It.Is<ProcessNoOpInboxCommand>(c => c.Inbox == envelope),
            cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_UnknownEvent_ShouldThrowUnknownEventTypeException()
    {
        // Arrange
        const string unknownEventType = "unknown.custom-event.v1";
        var envelope = CreateEnvelope(unknownEventType);

        // Act
        var act = async () => await _dispatcher.DispatchAsync(unknownEventType, "{}", envelope, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<UnknownEventTypeException>();
        exception.Which.EventType.Should().Be(unknownEventType);
    }

    [Fact]
    public async Task DispatchAsync_NullJsonPayload_ShouldThrowJsonException()
    {
        // Arrange
        var wireName = _registry.GetName<BookingReservationRequested>();
        var envelope = CreateEnvelope(wireName);

        // Act
        var act = async () => await _dispatcher.DispatchAsync(wireName, "null", envelope, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<JsonException>();
        exception.WithMessage($"*Invalid {nameof(BookingReservationRequested)} payload*");
    }

    [Fact]
    public async Task DispatchAsync_MalformedJsonPayload_ShouldThrowJsonException()
    {
        // Arrange
        var wireName = _registry.GetName<BookingReservationRequested>();
        var envelope = CreateEnvelope(wireName);

        // Act
        var act = async () => await _dispatcher.DispatchAsync(wireName, "{not-valid-json", envelope, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    private static InboxEnvelope CreateEnvelope(string messageType) => new(
        MessageId: Guid.NewGuid(),
        CorrelationId: Guid.NewGuid(),
        CausationId: Guid.NewGuid(),
        ConsumerName: "events-consumer-group",
        Topic: "booking-topic",
        Partition: 0,
        Offset: 42,
        MessageKey: "test-key",
        MessageType: messageType,
        Payload: "{}",
        Headers: "{}",
        ReceivedAt: DateTimeOffset.UtcNow);
}
