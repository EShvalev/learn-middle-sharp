using Microsoft.AspNetCore.Mvc;
using EventApi.Application.Events;
using EventApi.Application.Bookings;
using EventApi.Models.Bookings;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EventApi.Presentation
{
    [Route("api/[controller]")]
    [ApiController]

    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingService> _logger;

        public BookingsController(
            IBookingService bookingService,
            ILogger<BookingService> logger
            )
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        // GET api/<BookingsController>
        [HttpGet()]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var bookings = await _bookingService.GetBookingsAsync(ct);
            var response = bookings
                .Select(booking => new BookingDto(booking));
            return Ok(response);
        }

        // GET api/<BookingsController>/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id, ct);
            return Ok(new BookingDto(booking));
        }

        // POST api/events/{id}/book
        // Здесь id события, а не бронирования.
        [HttpPost("~/api/Events/{id}/book")]
        [ProducesResponseType(typeof(BookingCreateResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateAsync([FromRoute] Guid id, CancellationToken ct)
        {
            var booking = await _bookingService.CreateBookingAsync(id, ct);
            return AcceptedAtAction("GetById", new { id = booking.Id }, new BookingCreateResponse() { Id = booking.Id, EventId = booking.EventId, Status = booking.Status });
        }
    }
}
