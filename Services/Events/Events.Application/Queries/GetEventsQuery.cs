using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;

namespace Events.Application.Queries;

public sealed record GetEventsQuery(EventFilter dtoFilter) : IQuery<PaginatedResult<EventResponseDto>>;

internal sealed class GetEventsQueryHandler(IEventRepository eventRepository) : IQueryHandler<GetEventsQuery, PaginatedResult<EventResponseDto>>
{
    public async Task<PaginatedResult<EventResponseDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        DateTime? startInclusive = null;
        if (request.dtoFilter.From is { } fromDate)
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

        if (request.dtoFilter.To is { } toDate)
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

        int pageSize = request.dtoFilter.PageSize > 0
            ? Math.Min(request.dtoFilter.PageSize, 100)
            : 10;
        int page = request.dtoFilter.Page > 0
            ? Math.Min(request.dtoFilter.Page, 100000)
            : 1;

        var eventFilter = request.dtoFilter with { From = startInclusive, To = endExclusive, PageSize = pageSize, Page = page };
        var pagedEvents = await eventRepository.GetAllAsync(eventFilter, cancellationToken);
        return pagedEvents.Map(EventResponseDto.FromEntity);
    }
}
