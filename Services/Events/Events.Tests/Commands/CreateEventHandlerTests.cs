using AwesomeAssertions;
using Events.Application.Abstractions.Repositories;
using Events.Application.Commands;
using Events.Application.DTOs;
using Events.Domain.DomainEvents;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Moq;
using Xunit;

namespace Events.Tests.Commands;

public class CreateEventHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly CreateEventCommandHandler _handler;

    public CreateEventHandlerTests()
    {
        _handler = new CreateEventCommandHandler(_eventRepositoryMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithEmptyOrNullTitle_ShouldThrowValidationException(string? title)
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new EventCreateDto(
            Title: title,
            StartAt: futureDate,
            EndAt: futureDate.AddHours(2),
            TotalSeats: 10);
        var command = new CreateEventCommand(dto);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("title");

        _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEndAtBeforeStartAt_ShouldThrowValidationException()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new EventCreateDto(
            Title: "Test Event",
            StartAt: futureDate,
            EndAt: futureDate.AddHours(-1),
            TotalSeats: 10);
        var command = new CreateEventCommand(dto);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("endAt");

        _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEndAtEqualToStartAt_ShouldThrowValidationException()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new EventCreateDto(
            Title: "Test Event",
            StartAt: futureDate,
            EndAt: futureDate,
            TotalSeats: 10);
        var command = new CreateEventCommand(dto);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("startAt").And.ContainKey("endAt");

        _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidDates_ShouldThrowValidationException()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new EventCreateDto(
            Title: "Create Title",
            Description: "Create Description",
            StartAt: futureDate,
            EndAt: futureDate.AddDays(-1),
            TotalSeats: 1);
        var command = new CreateEventCommand(dto);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("endAt");

        _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithPastStartAt_ShouldThrowValidationException()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var dto = new EventCreateDto(
            Title: "Test Event",
            StartAt: pastDate,
            EndAt: pastDate.AddHours(2),
            TotalSeats: 10);
        var command = new CreateEventCommand(dto);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exceptionAssertion.Which.ValidationErrors.Should().ContainKey("startAt");

        _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTitleWhitespace_TrimsTitleAndCreatesEvent()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new EventCreateDto(
            Title: "  Test Event  ",
            StartAt: futureDate,
            EndAt: futureDate.AddHours(2),
            TotalSeats: 10);
        var command = new CreateEventCommand(dto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Title.Should().Be("Test Event");
        _eventRepositoryMock.Verify(repo => repo.Add(It.Is<Event>(e => e.Title == "Test Event")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnCreatedEvent()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new EventCreateDto(
            Title: "Title",
            Description: "Desc",
            StartAt: futureDate,
            EndAt: futureDate.AddHours(2),
            TotalSeats: 10,
            Price: 500m,
            Currency: "kzt");
        var command = new CreateEventCommand(dto);

        Event? capturedEvent = null;
        _eventRepositoryMock
            .Setup(repo => repo.Add(It.IsAny<Event>()))
            .Callback<Event>(e => capturedEvent = e);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Title.Should().Be("Title");
        result.Description.Should().Be("Desc");
        result.StartAt.Should().Be(futureDate);
        result.EndAt.Should().Be(futureDate.AddHours(2));
        result.TotalSeats.Should().Be(10);
        result.Price.Should().Be(500m);
        result.Currency.Should().Be("KZT");

        _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Once);
        capturedEvent.Should().NotBeNull();
        capturedEvent.PopDomainEvents().Should().ContainSingle(e => e is EventCreatedDomainEvent);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCallRepositoryAdd()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new EventCreateDto(
            Title: "Title",
            Description: "Desc",
            StartAt: futureDate,
            EndAt: futureDate.AddHours(2),
            TotalSeats: 10);
        var command = new CreateEventCommand(dto);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventRepositoryMock.Verify(
            repo => repo.Add(It.IsAny<Event>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new EventCreateDto(
            Title: "Title",
            Description: "Desc",
            StartAt: futureDate,
            EndAt: futureDate.AddHours(2),
            TotalSeats: 10);
        var command = new CreateEventCommand(dto);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
    }
}
