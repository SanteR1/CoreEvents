using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Data.Repositories;

internal sealed class EventRepository : IEventRepository
{
    private readonly EventsDbContext _context;

    public EventRepository(EventsDbContext context)
    {
        _context = context;
    }
    public async Task<PaginatedResult<Event>> GetAllAsync(EventFilter eventFilter, CancellationToken ct = default)
    {
        var entity = _context.Events
            .AsQueryable()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(eventFilter.Title))
        {
            if (_context.Database.IsNpgsql())
            {
                entity = entity.Where(e => EF.Functions.ILike(e.Title, $"%{eventFilter.Title}%"));
            }
            else
            {
                var titleLower = eventFilter.Title.ToLowerInvariant();
#pragma warning disable CA1862, CA1304, CA1311, RCS1155 // EF Core expression tree does not translate StringComparison overloads
                entity = entity.Where(e => e.Title.ToLower().Contains(titleLower));
#pragma warning restore CA1862, CA1304, CA1311, RCS1155
            }
        }

        if (eventFilter.From is not null)
        {
            entity = entity.Where(e => e.StartAt >= eventFilter.From.Value);
        }

        if (eventFilter.To is not null)
        {
            // Сервис передает в параметр "To" ИСКЛЮЧИТЕЛЬНУЮ (Exclusive) границу.
            // По этому использую строгое неравенство (<), а не (<=)
            entity = entity.Where(e => e.EndAt < eventFilter.To.Value);
        }

        var totalEvents = await entity.CountAsync(ct);

        if (totalEvents == 0)
        {
            return new PaginatedResult<Event>
            {
                Items = [],
                PageSize = eventFilter.PageSize,
                CurrentPage = eventFilter.Page,
                TotalCount = 0
            };
        }

        IReadOnlyList<Event> items = await entity
            .OrderByDescending(e => e.StartAt)
            .Skip((eventFilter.Page - 1) * eventFilter.PageSize)
            .Take(eventFilter.PageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Event>()
        {
            Items = items,
            PageSize = eventFilter.PageSize,
            CurrentPage = eventFilter.Page,
            TotalCount = totalEvents
        };
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Events.FindAsync([id], ct);
    }

    public async Task<List<Event>> GetTopEventsBySalesPercentageAsync(int take, CancellationToken ct = default)
    {
        return await _context.Events
                             .OrderByDescending(x => (double)(x.TotalSeats - x.AvailableSeats) / x.TotalSeats)
                             .Take(take)
                             .ToListAsync(ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Пробрасываем доменное исключение в слой Application
            throw new ConcurrencyException("Обнаружена конкурентная модификация данных.", ex);
        }
    }

    public void Add(Event entity)
    {
        _context.Events.Add(entity);
    }

    public void Update(Event entity)
    {
        _context.Events.Update(entity);
    }

    public void Delete(Event entity)
    {
        _context.Events.Remove(entity);
    }
}
