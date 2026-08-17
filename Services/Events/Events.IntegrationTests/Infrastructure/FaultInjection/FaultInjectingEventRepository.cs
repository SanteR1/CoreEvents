using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Domain.Entities;
using Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Events.IntegrationTests.Infrastructure.FaultInjection;

sealed class FaultInjectingEventRepository(IEventRepository inner, FaultInjectionState state, EventsDbContext dbContext) : IEventRepository
{
    public async Task<PaginatedResult<Event>> GetAllAsync(EventFilter eventFilter, CancellationToken ct = default)
    {
        return await inner.GetAllAsync(eventFilter, ct);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await CheckForTimeoutsAsync(cancellationToken);
        return await inner.GetByIdAsync(id, cancellationToken);
    }

    public async Task<List<Event>> GetTopEventsBySalesPercentageAsync(int take, CancellationToken cancellationToken = default)
    {
        await CheckForTimeoutsAsync(cancellationToken);
        return await inner.GetTopEventsBySalesPercentageAsync(take, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        await CheckForTimeoutsAsync(ct);

        dbContext.ChangeTracker.DetectChanges();

        var modifiedBookings = dbContext.ChangeTracker.Entries<Event>()
            .Where(e => e.State == EntityState.Modified)
            .Select(e => e.Entity)
            .ToList();

        bool shouldSimulateFailure = false;

        //foreach (var booking in modifiedBookings)
        //{
        //    // Пропускаем проверку, если это успешный откат (Reject)
        //    if (booking.Status == BookingStatus.Rejected)
        //        continue;

        //    // Проверка 1: Падение по EventId
        //    if (booking.EventId == state.TargetEventIdForFailures)
        //    {
        //        shouldSimulateFailure = true;
        //        break;
        //    }

        //    // Проверка 2: Падение по конкретному BookingId
        //    if (state.ShouldFailForBooking(booking.Id))
        //    {
        //        shouldSimulateFailure = true;
        //        break;
        //    }
        //}

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

    public void Add(Event booking)
    {
        inner.Add(booking);
    }

    public void Delete(Event booking)
    {
        inner.Delete(booking);
    }

    public void Update(Event booking)
    {
        inner.Update(booking);
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
