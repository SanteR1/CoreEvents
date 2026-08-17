using Bookings.Application.Abstractions;
using Bookings.Application.Commands;
using Bookings.Application.DTOs;
using Bookings.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Controllers;

[Route("[controller]")]
[ApiController]
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

    [HttpPost("{id:guid}/book")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var command = new CreateBookingCommand(
            UserId: userId.GetValueOrDefault(),
            EventId: id
        );

        var createdBooking = await _mediator.Send(command, ct);
        return AcceptedAtRoute(
            "GetBookingStatus",
            new { id = createdBooking.Id },
            createdBooking
        );
    }

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

    [HttpDelete("{id:guid}")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var userRole = _userContext.Role;

        var command = new CancelBookingByUserCommand(
            id,
            userId.GetValueOrDefault(),
            userRole.GetValueOrDefault()
            );
        await _mediator.Send(command, ct);
        //var cancelBooking = await _mediator.Send(command, ct);
        //return AcceptedAtRoute(
        //    "GetBookingStatus",
        //    new { id = cancelBooking },
        //    cancelBooking
        //);
        return NoContent();
    }
}
