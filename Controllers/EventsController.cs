using CoreEvents.Models.Domain;
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

       
        [ProducesResponseType(typeof(ActionResult<IEnumerable<EventEntity>>), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet]
        public ActionResult<IEnumerable<EventEntity>> GetAll() => Ok(_eventService.GetEvents());

        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpGet("{id:guid}")]
        public ActionResult<EventEntity> GetById(Guid id)
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

        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [HttpPost]
        public ActionResult<EventEntity> Create([FromBody] EventEntity entity)
        {
            try
            {
                _eventService.CreateEvent(entity);
                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpPut("{id:guid}")]
        public ActionResult<EventEntity> Put(Guid id, [FromBody] EventEntity entity)
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


        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ActionResult<EventEntity>), StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpDelete("{id:guid}")]
        public ActionResult<EventEntity> Delete(Guid id)
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
