using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreEvents.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<EventResponseDto>> GetAll() => Ok(_eventService.GetEvents());

        [HttpGet("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public ActionResult<EventResponseDto> GetById(Guid id)
        {
            var result = _eventService.GetEventById(id);
            return Ok(result);
        }

        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public ActionResult<EventResponseDto> Create([FromBody] EventCreateDto entity)
        {
            var createdEvent = _eventService.CreateEvent(entity);

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
        public IActionResult Put(Guid id, [FromBody] EventCreateDto entity)
        {
            _eventService.UpdateEvent(id, entity);
            return NoContent();
        }
        
        [HttpDelete("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult Delete(Guid id)
        {
            _eventService.DeleteEvent(id);
            return NoContent();
        }
    }
}
