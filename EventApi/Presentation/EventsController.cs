using EventApi.Application.Events;
using EventApi.Models.Bookings;
using EventApi.Models.Events;
using Microsoft.AspNetCore.Mvc;


namespace EventApi.Presentation
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventService eventService,
            ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        [HttpGet()]
        public IActionResult GetEvents([FromQuery] string? title, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            return Ok(_eventService.GetEvents(title, from, to, page, pageSize));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
        public IActionResult GetById(Guid id)
        {
            var evnt = _eventService.GetById(id);
            return evnt != null ? Ok(evnt) : NotFound();
        }

        [HttpPost()]
        [ProducesResponseType(typeof(EventCreateResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status422UnprocessableEntity)]
        public IActionResult Create([FromBody] EventDto eventdto)
        {
            var evnt = Event.CreateNew(
                title: eventdto.Title,
                description: eventdto.Description,
                startAt: eventdto.StartAt,
                endAt: eventdto.EndAt
            );
            var id = _eventService.Create(evnt);

            return CreatedAtAction(nameof(GetById), new EventCreateResponse { Id = evnt.Id }, evnt);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status422UnprocessableEntity)]
        public IActionResult Update(Guid id, EventDto eventdto)
        {
            var evnt = _eventService.GetById(id);
            if (evnt == null)
            {
                return NotFound();
            }

            evnt.Title = eventdto.Title;
            evnt.Description = eventdto.Description;
            evnt.StartAt = eventdto.StartAt;
            evnt.EndAt = eventdto.EndAt;

            _eventService.Update(id, evnt);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
        public IActionResult Delete(Guid id)
        {
            var evnt = _eventService.GetById(id);
            if (evnt == null)
            {
                return NotFound();
            }

            _eventService.Delete(id);

            return NoContent();
        }
    }
}
