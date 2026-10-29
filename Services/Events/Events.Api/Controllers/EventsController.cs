using Asp.Versioning;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/events")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>Получение каталога событий с пагинацией и фильтрацией.</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PaginatedResult<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<EventResponseDto>>> GetAll(
        [FromQuery] EventFilter filter,
        CancellationToken ct)
    {
        return Ok(await _eventService.GetAllEventsAsync(filter, ct));
    }

    /// <summary>Получение детальной информации о событии по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _eventService.GetEventByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Топ-10 событий по проценту продаж билетов.</summary>
    [HttpGet("top")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetTopEventsBySalesPercentageAsync(CancellationToken ct)
    {
        var result = await _eventService.GetTopEventsBySalesPercentageAsync(ct);
        return Ok(result);
    }

    /// <summary>Создание нового события (только Admin). Задает начальные дату, количество мест, цену и валюту (по умолчанию KZT).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponseDto>> Create(
        [FromBody] EventCreateDto entity,
        CancellationToken ct)
    {
        var createdEvent = await _eventService.CreateEventAsync(entity, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdEvent.Id },
            createdEvent
        );
    }

    /// <summary>Обновление метаданных, дат или цены события (только Admin). Возвращает обновленное состояние события.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> Put(
        Guid id,
        [FromBody] EventUpdateDto entity,
        CancellationToken ct)
    {
        var updated = await _eventService.UpdateEventAsync(id, entity, ct);
        return Ok(updated);
    }

    /// <summary>Удаление / отмена события (только Admin). Переводит IsActive = false и отправляет EventCancelled в Outbox.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _eventService.DeleteEventAsync(id, ct);
        return NoContent();
    }

    /// <summary>Семантический эндпоинт отмены события организатором (только Admin).</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEvent(Guid id, CancellationToken ct)
    {
        return await Delete(id, ct);
    }

    /// <summary>Инспекция всех резерваций мест на событии (только Admin).</summary>
    [HttpGet("{id:guid}/reservations")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SeatReservationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SeatReservationResponseDto>>> GetReservations(
        Guid id,
        [FromServices] ISeatReservationRepository reservationRepo,
        CancellationToken ct)
    {
        var reservations = await reservationRepo.GetByEventIdAsync(id, ct);
        return Ok(reservations.Select(SeatReservationResponseDto.FromEntity).ToList());
    }
}
