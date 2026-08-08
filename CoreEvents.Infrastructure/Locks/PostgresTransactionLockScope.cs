using CoreEvents.Application.Interfaces.Locks;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoreEvents.Infrastructure.Locks;

internal sealed class PostgresTransactionLockScope : ILockScope
{
    private readonly IDbContextTransaction _transaction;
    private bool _isCompleted;

    public PostgresTransactionLockScope(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CompleteAsync(CancellationToken ct = default)
    {
        if (_isCompleted) return;

        await _transaction.CommitAsync(ct);
        _isCompleted = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isCompleted)
        {
            await _transaction.RollbackAsync();
        }

        await _transaction.DisposeAsync();
    }
}
