using System.Data;
using Events.Application.Abstractions.Persistence;
using Events.Domain.DomainEvents;
using Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Events.Infrastructure.Data;

internal sealed class UnitOfWork(EventsDbContext dbContext) : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private IDbContextTransaction? _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction != null || dbContext.Database.CurrentTransaction != null;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP003:Dispose previous before re-assigning", Justification = "Active transaction guard throws if not null")]
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct)
    {
        if (_currentTransaction != null || dbContext.Database.CurrentTransaction != null)
            throw new InvalidOperationException("Транзакция уже открыта в текущем Unit of Work.");

        _currentTransaction = await dbContext.Database.BeginTransactionAsync(isolationLevel, ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("Нет активной транзакции для фиксации.");

        try
        {
            await _currentTransaction.CommitAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(ct);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }

            // Очистка ChangeTracker при ретрае: исключает переиспользование устаревших сущностей при повторе Polly
            dbContext.ChangeTracker.Clear();
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return dbContext.SaveChangesAsync(ct);
    }

    public IReadOnlyList<IDomainEvent> CollectDomainEvents()
    {
        var events = dbContext.ChangeTracker
            .Entries<Event>()
            .SelectMany(entry => entry.Entity.PopDomainEvents())
            .OrderBy(e => e.OccurredAt)
            .ToList();

        return events;
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _currentTransaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}
