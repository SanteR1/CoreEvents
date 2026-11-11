using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Domain.Entities;
using Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Events.IntegrationTests.Infrastructure.FaultInjection;

sealed class FaultInjectingEventRepository(IEventRepository inner, FaultInjectionState state) : IEventRepository
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

    public async Task<IReadOnlyList<Event>> GetTopEventsBySalesPercentageAsync(int take, CancellationToken cancellationToken = default)
    {
        await CheckForTimeoutsAsync(cancellationToken);
        return await inner.GetTopEventsBySalesPercentageAsync(take, cancellationToken);
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
