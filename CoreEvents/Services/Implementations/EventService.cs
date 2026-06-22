using CoreEvents.Data.Repositories.Interfaces;
using CoreEvents.Middleware;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Interfaces;

namespace CoreEvents.Services.Implementations
{
    internal sealed class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<PaginatedResult<EventResponseDto>> GetAllEventsAsync(EventFilter dtoFilter, CancellationToken ct = default)
        {
            DateTime? startInclusive = null;
            if (dtoFilter.From is { } fromDate)
            {
                startInclusive = fromDate.Kind switch
                {
                    // Если пришло локальное время — конвертируем его в UTC
                    DateTimeKind.Local => fromDate.ToUniversalTime(),

                    // Если пришло Unspecified — вешаем ярлык UTC
                    DateTimeKind.Unspecified => DateTime.SpecifyKind(fromDate, DateTimeKind.Utc),

                    // Если уже UTC — ничего не делаем
                    DateTimeKind.Utc => fromDate,

                    _ => fromDate
                };
            }

            DateTime? endExclusive = null;

            if (dtoFilter.To is { } toDate)
            {
                // 1. Сначала проверяем намерения пользователя по ЕГО времени
                bool isFullDay = toDate.TimeOfDay == TimeSpan.Zero;

                // 2. Безопасно приводим саму дату к UTC, не искажая часы, если это Unspecified
                DateTime utcToDate = toDate.Kind switch
                {
                    DateTimeKind.Local => toDate.ToUniversalTime(),
                    DateTimeKind.Unspecified => DateTime.SpecifyKind(toDate, DateTimeKind.Utc),
                    DateTimeKind.Utc => toDate,
                    _ => toDate
                };

                // 3. Добавляем сдвиги к уже нормализованной UTC-дате
                if (isFullDay)
                {
                    // Пользователь просил весь день (до конца суток)
                    endExclusive = utcToDate.AddDays(1);
                }
                else
                {
                    // Пользователь передал точное время, добавляем микросекунду для БД
                    endExclusive = utcToDate.AddMicroseconds(1);
                }
            }

            int pageSize = dtoFilter.PageSize > 0
                ? Math.Min(dtoFilter.PageSize, 100)
                : 10;
            int page = dtoFilter.Page > 0
                ? Math.Min(dtoFilter.Page, 10)
                : 1;

            var eventFilter = dtoFilter with {From = startInclusive, To = endExclusive,PageSize = pageSize, Page = page };
            var pagedEvents = await _eventRepository.GetAllAsync(eventFilter, ct);
            return pagedEvents.Map(EventResponseDto.FromEntity);
        }

        public async Task<EventResponseDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            var eventEntity = await _eventRepository.GetByIdAsync(id, ct);
            if (eventEntity == null)
                throw new NotFoundException($"Событие с ID {id} не найдено.");

            return EventResponseDto.FromEntity(eventEntity);
        }

        public async Task<EventResponseDto> CreateEventAsync(EventCreateDto entityDto, CancellationToken ct = default)
        {
            var entity = Event.Create(
                title: entityDto.Title!,
                startAt: entityDto.StartAt!.Value,
                endAt: entityDto.EndAt!.Value,
                totalSeats: entityDto.TotalSeats!.Value,
                description: entityDto.Description);

            _eventRepository.Add(entity);
            await _eventRepository.SaveChangesAsync(ct);

            return EventResponseDto.FromEntity(entity);
        }
        public async Task<EventResponseDto> UpdateEventAsync(Guid id, EventUpdateDto entityDto, CancellationToken ct = default)
        {
            var existing = await _eventRepository.GetByIdAsync(id, ct);
            if (existing == null) throw new NotFoundException("Событие не найдено.");

            existing.Update(
                entityDto.Title,
                entityDto.StartAt,
                entityDto.EndAt,
                entityDto.Description
                );

            await _eventRepository.SaveChangesAsync(ct);
            return EventResponseDto.FromEntity(existing);
        }

        public async Task<bool> DeleteEventAsync(Guid id, CancellationToken ct = default)
        {
            var existing = await _eventRepository.GetByIdAsync(id, ct);
            if (existing == null)
                return false;

            _eventRepository.Delete(existing);
            await _eventRepository.SaveChangesAsync(ct);
            return true;
        }
    }
}
