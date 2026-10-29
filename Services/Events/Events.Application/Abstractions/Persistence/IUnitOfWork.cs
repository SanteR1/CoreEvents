using System.Data;
using Events.Domain.DomainEvents;

namespace Events.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct);
    Task CommitTransactionAsync(CancellationToken ct);
    Task RollbackTransactionAsync(CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
    IReadOnlyList<IDomainEvent> CollectDomainEvents();
    bool HasActiveTransaction { get; }
}
