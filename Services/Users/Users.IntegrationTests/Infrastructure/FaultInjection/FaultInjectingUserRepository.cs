using Microsoft.EntityFrameworkCore;
using Users.Application.Interfaces.Repositories;
using Users.Domain.Entities;
using Users.Infrastructure.Data;

namespace Users.IntegrationTests.Infrastructure.FaultInjection;

internal sealed class FaultInjectingUserRepository(
    IUserRepository inner,
    FaultInjectionState state,
    UsersDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await CheckForTimeoutsAsync(ct);

        return await inner.GetByIdAsync(id, ct);
    }

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        await CheckForTimeoutsAsync(ct);

        return await inner.GetByUserNameAsync(userName, ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        await CheckForTimeoutsAsync(ct);

        dbContext.ChangeTracker.DetectChanges();

        bool shouldSimulateFailure = false;

        if (shouldSimulateFailure)
        {
            // Кастомная ошибка (если тест хочет выбросить что-то специфичное)
            if (state.CustomExceptionToThrow != null)
            {
                throw state.CustomExceptionToThrow;
            }

            // Ошибка конкурентного доступа (полезно, если используете RowVersion в EF Core)
            if (state.SimulateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException("Simulated optimistic concurrency exception.");
            }

            // Классическая ошибка (по умолчанию)
            throw new DbUpdateException("Simulated transient database failure for concurrent rollback test.");
        }

        return await inner.SaveChangesAsync(ct);
    }

    public void Add(User entity)
    {
        inner.Add(entity);
    }

    // Вспомогательный метод для имитации зависания БД
    private async Task CheckForTimeoutsAsync(CancellationToken ct)
    {
        if (state.SimulateDatabaseTimeout)
        {
            // Task.Delay сработает и выбросит TaskCanceledException, 
            // если входящий CancellationToken истечет раньше, чем DatabaseDelay
            await Task.Delay(state.DatabaseDelay, ct);
        }
    }
}
