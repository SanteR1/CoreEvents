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
        var entity = ApplyFilters(_context.Events.AsNoTracking(), eventFilter, _context.Database.IsNpgsql());

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

    public async Task<IReadOnlyList<Event>> GetTopEventsBySalesPercentageAsync(int take, CancellationToken ct = default)
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

    private static IQueryable<Event> ApplyFilters(IQueryable<Event> query, EventFilter eventFilter, bool isNpgsql)
    {
        if (!string.IsNullOrWhiteSpace(eventFilter.Title))
        {
            if (isNpgsql)
            {
                query = query.Where(e => EF.Functions.ILike(e.Title, $"%{eventFilter.Title}%"));
            }
            else
            {
                var titleLower = eventFilter.Title.ToLowerInvariant();
#pragma warning disable CA1862, CA1304, CA1311, RCS1155, MA0011
                query = query.Where(e => e.Title.ToLower().Contains(titleLower));
#pragma warning restore CA1862, CA1304, CA1311, RCS1155, MA0011
            }
        }

        if (eventFilter.From is not null)
        {
            query = query.Where(e => e.StartAt >= eventFilter.From.Value);
        }

        if (eventFilter.To is not null)
        {
            query = query.Where(e => e.EndAt < eventFilter.To.Value);
        }

        if (eventFilter.MinPrice is not null)
        {
            query = query.Where(e => e.Price >= eventFilter.MinPrice.Value);
        }

        if (eventFilter.MaxPrice is not null)
        {
            query = query.Where(e => e.Price <= eventFilter.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(eventFilter.Currency))
        {
            query = query.Where(e => e.Currency == eventFilter.Currency);
        }

        if (eventFilter.IsActive is not null)
        {
            query = query.Where(e => e.IsActive == eventFilter.IsActive.Value);
        }

        return query;
    }
}
