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
            var result = _eventService.GetEvents(title, from, to, page, pageSize)
                .Items.Select(evnt => new EventDto(evnt));
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(Guid id)
        {
            var evnt = _eventService.GetById(id);
            return Ok(new EventDto(evnt));
        }

        [HttpPost()]
        [ProducesResponseType(typeof(EventCreateResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public IActionResult Create([FromBody] EventCreateUpdateRequest eventReq)
        {
            var evnt = Event.CreateNew(
                title: eventReq.Title,
                description: eventReq.Description,
                startAt: eventReq.StartAt,
                endAt: eventReq.EndAt
            );
            var id = _eventService.Create(evnt);

            return CreatedAtAction(nameof(GetById), new { id = evnt.Id }, new EventCreateResponse { Id = evnt.Id });
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public IActionResult Update(Guid id, EventCreateUpdateRequest eventReq)
        {
            var evnt = _eventService.GetById(id);
            if (evnt == null)
            {
                return NotFound();
            }

            evnt.Title = eventReq.Title;
            evnt.Description = eventReq.Description;
            evnt.StartAt = eventReq.StartAt;
            evnt.EndAt = eventReq.EndAt;

            _eventService.Update(id, evnt);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
