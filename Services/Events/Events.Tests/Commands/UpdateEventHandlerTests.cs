using AwesomeAssertions;
using Events.Application.Abstractions.Repositories;
using Events.Application.Commands;
using Events.Application.DTOs;
using Events.Application.Exceptions;
using Events.Domain.DomainEvents;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Events.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Events.Tests.Commands;

public class UpdateEventHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly UpdateEventCommandHandler _handler;

    public UpdateEventHandlerTests()
    {
        _handler = new UpdateEventCommandHandler(_eventRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NonExistingId_ShouldThrowEventNotFoundException()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var updateDto = new EventUpdateDto(
            Title: "Update Title",
            StartAt: futureDate,
            EndAt: futureDate.AddHours(2),
            Description: "Update Description");
        var command = new UpdateEventCommand(expectedId, updateDto);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(expectedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<EventNotFoundException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Event.NotFound");
        exceptionAssertion.Which.ErrorData.Should().BeEquivalentTo(new { parameter = "Id", value = expectedId.ToString() });

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(expectedId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingId_ShouldModifyEvent()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var updateDto = new EventUpdateDto(
            Title: "Title Update",
            Description: "Updated Description",
            StartAt: DateTime.UtcNow.AddDays(2),
            EndAt: DateTime.UtcNow.AddDays(3));
        var command = new UpdateEventCommand(existingEvent.Id, updateDto);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(existingEvent.Id);
        result.Title.Should().Be(updateDto.Title);
        result.Description.Should().Be(updateDto.Description);
        result.StartAt.Should().Be(updateDto.StartAt);
        result.EndAt.Should().Be(updateDto.EndAt);
        result.TotalSeats.Should().Be(existingEvent.TotalSeats);
        result.AvailableSeats.Should().Be(existingEvent.AvailableSeats);

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
        existingEvent.PopDomainEvents().Should().ContainSingle(e => e is EventUpdatedDomainEvent);
    }

    [Fact]
    public async Task Handle_WhenPriceOrCurrencyChanged_ShouldIncrementPriceVersionAndRaiseEvent()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create(price: 100m, currency: "KZT");
        var initialPriceVersion = existingEvent.PriceVersion;

        var updateDto = new EventUpdateDto(
            Title: existingEvent.Title,
            Description: existingEvent.Description,
            StartAt: existingEvent.StartAt,
            EndAt: existingEvent.EndAt,
            Price: 200m,
            Currency: "usd");
        var command = new UpdateEventCommand(existingEvent.Id, updateDto);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Price.Should().Be(200m);
        result.Currency.Should().Be("USD");
        existingEvent.PriceVersion.Should().Be(initialPriceVersion + 1);

        var domainEvents = existingEvent.PopDomainEvents();
        var updatedEvent = domainEvents.OfType<EventUpdatedDomainEvent>().Single();
        updatedEvent.UnitPrice.Should().Be(200m);
        updatedEvent.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_WithEndAtBeforeStartAt_ShouldThrowValidationException()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var updateDto = new EventUpdateDto(
            Title: "Update Title",
            Description: "Update Description",
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(-1));
        var command = new UpdateEventCommand(existingEvent.Id, updateDto);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("endAt");

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullTitle_ShouldThrowValidationException()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var updateDto = new EventUpdateDto(
            Title: null,
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(1).AddHours(2));
        var command = new UpdateEventCommand(existingEvent.Id, updateDto);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("title");

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPastStartAt_ShouldThrowValidationException()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var updateDto = new EventUpdateDto(
            Title: "Updated Event",
            StartAt: pastDate,
            EndAt: pastDate.AddHours(2));
        var command = new UpdateEventCommand(existingEvent.Id, updateDto);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("startAt");

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidData_UpdatesEvent()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var newFutureDate = DateTime.UtcNow.AddDays(2);
        var updateDto = new EventUpdateDto(
            Title: "Updated Event",
            Description: "Updated Description",
            StartAt: newFutureDate,
            EndAt: newFutureDate.AddHours(3));
        var command = new UpdateEventCommand(existingEvent.Id, updateDto);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(existingEvent.Id);
        result.Title.Should().Be(updateDto.Title);
        result.Description.Should().Be(updateDto.Description);
        result.StartAt.Should().Be(newFutureDate);
        result.EndAt.Should().Be(newFutureDate.AddHours(3));

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldPassTokenToRepository()
    {
        // Arrange
        var existEvent = TestEventFactory.Create();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var updateDto = new EventUpdateDto(
            Title: "Updated Event",
            Description: "Updated Description",
            StartAt: DateTime.UtcNow.AddDays(2),
            EndAt: DateTime.UtcNow.AddDays(2).AddHours(3));
        var command = new UpdateEventCommand(existEvent.Id, updateDto);

        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existEvent);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _eventRepositoryMock.Verify(
            repo => repo.GetByIdAsync(existEvent.Id, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var existEvent = TestEventFactory.Create();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cancellationToken = cts.Token;
        var updateDto = new EventUpdateDto(
            Title: "Updated Event",
            Description: "Updated Description",
            StartAt: DateTime.UtcNow.AddDays(2),
            EndAt: DateTime.UtcNow.AddDays(2).AddHours(3));
        var command = new UpdateEventCommand(existEvent.Id, updateDto);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();

        _eventRepositoryMock.Verify(
            repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

