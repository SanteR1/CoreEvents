using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Data.Repositories
{
    internal sealed class EventRepository : IEventRepository
    {
        private readonly EventsDbContext _context;

        public EventRepository(EventsDbContext context)
        {
            _context = context;
        }
        public async Task<PaginatedResult<Event>> GetAllAsync(EventFilter eventFilter, CancellationToken cancellationToken = default)
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
                    entity = entity.Where(e => e.Title.ToLower().Contains(eventFilter.Title.ToLower()));
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

            var totalEvents = await entity.CountAsync(cancellationToken);

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
                .ToListAsync(cancellationToken);

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
}
