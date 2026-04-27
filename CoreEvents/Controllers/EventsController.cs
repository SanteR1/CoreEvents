using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CoreEvents.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IBookingService _bookingService;

        public EventsController(IEventService eventService, IBookingService bookingService)
        {
            _eventService = eventService;
            _bookingService = bookingService;
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetAll([FromQuery] EventFilter filter)
        {
            return Ok(await _eventService.GetEvents(filter));
        }

        [HttpGet("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventResponseDto>> GetById(Guid id)
        {
            var result = await _eventService.GetEventById(id);
            return Ok(result);
        }

        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EventResponseDto>> Create([FromBody] EventCreateDto entity)
        {
            var createdEvent = await _eventService.CreateEvent(entity);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdEvent.Id },
                createdEvent
            );
        }

        [HttpPut("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(Guid id, [FromBody] EventCreateDto entity)
        {
            await _eventService.UpdateEvent(id, entity);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _eventService.DeleteEvent(id);
            return NoContent();
        }

        [HttpPost("{id:guid}/book")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingResponseDto>> CreateBooking([FromRoute] Guid id, CancellationToken ct)
        {
            var createdBooking = await _bookingService.
                CreateBookingAsync(new BookingCreateDto(id), ct);

            return AcceptedAtRoute(
                "GetBookingStatus",
                new { id = createdBooking.Id },
                createdBooking
            );
        }
    }
}
