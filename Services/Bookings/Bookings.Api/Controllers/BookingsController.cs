using Asp.Versioning;
using Bookings.Application.Abstractions;
using Bookings.Application.Commands;
using Bookings.Application.DTOs;
using Bookings.Application.Queries;
using CoreEvents.Shared.Contracts.Events;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IUserContext _userContext;
    private readonly IMediator _mediator;

    public BookingsController(
        IMediator mediator,
        IUserContext userContext)
    {
        _mediator = mediator;
        _userContext = userContext;
    }

    /// <summary>Создание заявки на бронирование (202 Accepted). Принимает опциональное количество мест seats (по умолчанию 1).</summary>
    [HttpPost("{id:guid}/book")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(
        [FromRoute] Guid id,
        [FromBody] CreateBookingRequestDto? request,
        CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var command = new CreateBookingCommand(
            UserId: userId.GetValueOrDefault(),
            EventId: id,
            Seats: request?.Seats ?? 1
        );

        var createdBooking = await _mediator.Send(command, ct);
        return AcceptedAtRoute(
            "GetBookingStatus",
            new { id = createdBooking.Id },
            createdBooking
        );
    }

    /// <summary>Получение статуса и финансового снимка бронирования по ID.</summary>
    [HttpGet("{id:guid}", Name = "GetBookingStatus")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetBookingByIdQuery(id, _userContext.UserId.GetValueOrDefault(), _userContext.Role.GetValueOrDefault());
        var result = await _mediator.Send(query, ct);

        return Ok(result);
    }

    /// <summary>Получение списка бронирований текущего пользователя с постраничной навигацией и фильтром по статусу.</summary>
    [HttpGet]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PaginatedResult<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<BookingResponseDto>>> GetMyBookings(
        [FromQuery] BookingFilter filter,
        CancellationToken ct)
    {
        var userId = _userContext.UserId.GetValueOrDefault();
        var userRole = _userContext.Role.GetValueOrDefault();
        var query = new GetUserBookingsQuery(userId, userRole, filter);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>Отмена подтвержденного бронирования (202 Accepted). Запрещена в статусе Pending (409 Conflict).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponseDto>> Delete(
        [FromRoute] Guid id,
        [FromBody] CancelBookingRequestDto? request,
        CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var userRole = _userContext.Role;

        var command = new CancelBookingByUserCommand(
            id,
            userId.GetValueOrDefault(),
            userRole.GetValueOrDefault(),
            request?.Reason ?? CancellationReason.UserCancelled
        );
        var result = await _mediator.Send(command, ct);
        return Accepted(result);
    }
}
