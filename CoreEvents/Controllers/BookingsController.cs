using CoreEvents.Models.DTOs;
using CoreEvents.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreEvents.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet("{id:guid}", Name = "GetBookingStatus")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingResponseDto>> GetById(Guid id, CancellationToken ct)
        {
            var result = await _bookingService.GetBookingByIdAsync(id, ct);
            return Ok(result);
        }
    }
}
