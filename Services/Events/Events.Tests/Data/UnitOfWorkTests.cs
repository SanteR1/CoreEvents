using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Events.Domain.DomainEvents;
using Events.Domain.Entities;
using Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Events.Tests.Data;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP001:Dispose created", Justification = "In-memory test db context")]
public sealed class UnitOfWorkTests : IDisposable
{
    private readonly EventsDbContext _dbContext;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy")
            .Options;

        _dbContext = new EventsDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    [Fact]
    public void CollectDomainEvents_ShouldExtractEvents_SortByOccurredAt_AndClearEntityQueue()
    {
        // Arrange
        var t0 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var t1 = new DateTimeOffset(2026, 1, 1, 10, 5, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 1, 1, 10, 10, 0, TimeSpan.Zero);

        var eventA = Event.Create("Event A", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), t1, totalSeats: 10);
        var eventB = Event.Create("Event B", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), t0, totalSeats: 20);
        var eventC = Event.Create("Event C", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), t2, totalSeats: 30);

        _dbContext.Events.Attach(eventA);
        _dbContext.Events.Attach(eventB);
        _dbContext.Events.Attach(eventC);

        // Act - первый сбор
        var collected = _unitOfWork.CollectDomainEvents();

        // Assert
        collected.Should().HaveCount(3);
        collected[0].OccurredAt.Should().Be(t0, "События должны быть отсортированы строго хронологически");
        collected[1].OccurredAt.Should().Be(t1);
        collected[2].OccurredAt.Should().Be(t2);

        // Act - повторный сбор
        var secondCollect = _unitOfWork.CollectDomainEvents();

        // Assert - очередь очищена
        secondCollect.Should().BeEmpty("PopDomainEvents должен очистить очередь событий сущностей");
    }

    [Fact]
    public async Task RollbackTransactionAsync_ShouldClearChangeTracker_EvenIfTransactionNull()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var @event = Event.Create("Tracked Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), now, totalSeats: 10);
        _dbContext.Events.Add(@event);

        _dbContext.ChangeTracker.Entries().Should().NotBeEmpty();

        // Act
        await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);

        // Assert
        _dbContext.ChangeTracker.Entries().Should().BeEmpty(
            "RollbackTransactionAsync обязан вызвать ChangeTracker.Clear() для очистки устаревших сущностей при повторах Polly");
    }

    [Fact]
    public async Task CommitTransactionAsync_WhenNoActiveTransaction_ShouldThrowInvalidOperationException()
    {
        // Act
        var act = () => _unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Нет активной транзакции для фиксации*");
    }

    [Fact]
    public void HasActiveTransaction_ShouldBeFalse_Initially()
    {
        _unitOfWork.HasActiveTransaction.Should().BeFalse();
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _dbContext.Dispose();
    }
}
