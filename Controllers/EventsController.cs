using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreEvents.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController: ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

       
        [ProducesResponseType(typeof(ActionResult<IEnumerable<EventResponseDto>>), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet]
        public ActionResult<IEnumerable<EventResponseDto>> GetAll() => Ok(_eventService.GetEvents());

        [ProducesResponseType(typeof(ActionResult<EventResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ActionResult<EventResponseDto>), StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpGet("{id:guid}")]
        public ActionResult<EventResponseDto> GetById(Guid id)
        {
            try
            {
                _eventService.GetEventById(id);
                return Ok(_eventService.GetEventById(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [ProducesResponseType(typeof(ActionResult<EventResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ActionResult<EventResponseDto>), StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        public ActionResult<EventResponseDto> Create([FromBody] EventCreateDto entity)
        {
            try
            {
                var createdEvent = _eventService.CreateEvent(entity);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = createdEvent.Id },
                    createdEvent
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpPut("{id:guid}")]
        public IActionResult Put(Guid id, [FromBody] EventCreateDto entity)
        {
            try
            {
                _eventService.UpdateEvent(id, entity);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpDelete("{id:guid}")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                _eventService.DeleteEvent(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
