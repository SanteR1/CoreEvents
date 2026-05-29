using CoreEvents.Data.DataAccess;
using CoreEvents.Middleware;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.Services.Implementations
{
    internal sealed class EventService : IEventService
    {
        private readonly AppDbContext _context;
        public EventService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult> GetAllEventsAsync(EventFilter eventFilter, CancellationToken cancellationToken = default)
        {
            var entity = _context.Events.AsQueryable();
            if (!string.IsNullOrWhiteSpace(eventFilter.Title))
            {
                if(_context.Database.IsNpgsql())
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
                var toDate = eventFilter.To.Value;
                if (toDate.TimeOfDay == TimeSpan.Zero)
                {
                    toDate = toDate.AddDays(1).AddTicks(-1);
                }
                entity = entity.Where(e => e.EndAt <= toDate);
            }

            var totalEvents = await entity.CountAsync(cancellationToken);

            var items = await entity
                .OrderByDescending(e => e.StartAt)
                .Skip((eventFilter.Page - 1) * eventFilter.PageSize)
                .Take(eventFilter.PageSize)
                .Select(EventResponseDto.ToDto)
                .ToListAsync(cancellationToken);

            return new PaginatedResult()
            {
                Items = items,
                PageSize = eventFilter.PageSize,
                CurrentPage = eventFilter.Page,
                TotalCount = totalEvents
            };
        }

        public async Task<EventResponseDto> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var dto = await _context.Events
                .Where(e => e.Id == id)
                .Select(EventResponseDto.ToDto)
                .FirstOrDefaultAsync(cancellationToken);

            if (dto == null)
                throw new NotFoundException($"Событие с ID {id} не найдено.");

            return dto;
        }

        public async Task<EventResponseDto> CreateEventAsync(EventCreateDto entityDto, CancellationToken cancellationToken = default)
        {
            var entity = Event.Create(
                title: entityDto.Title!,
                startAt: entityDto.StartAt!.Value,
                endAt: entityDto.EndAt!.Value,
                totalSeats: entityDto.TotalSeats!.Value,
                description: entityDto.Description);

            await _context.Events
                .AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return EventResponseDto.FromEntity(entity);
        }
        public async Task<EventResponseDto> UpdateEventAsync(Guid id, EventUpdateDto entityDto, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Events.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (existing == null) throw new NotFoundException("Событие не найдено.");

            existing.Update(
                entityDto.Title,
                entityDto.StartAt,
                entityDto.EndAt,
                entityDto.Description
                );

            await _context.SaveChangesAsync(cancellationToken);
            return EventResponseDto.FromEntity(existing);
        }

        public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Events.FirstOrDefaultAsync(x=> x.Id == id, cancellationToken);
            if (existing == null) //throw new NotFoundException("Событие не найдено.");
                return false;

            _context.Events.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
