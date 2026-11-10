using EventApi.Application.Events;
using Microsoft.AspNetCore.Mvc;


namespace EventApi.Presentation.Events
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
        public async Task<IActionResult> GetEvents([FromQuery] string? title, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _eventService.GetEventsAsync(title, from, to, page, pageSize, ct);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var evnt = await _eventService.GetEventByIdAsync(id, ct);
            return Ok(new EventDto(evnt));
        }

        [HttpPost()]
        [ProducesResponseType(typeof(EventCreateResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateEventDto eventReq, CancellationToken ct)
        {
            var dto = await _eventService.CreateEventAsync(eventReq, ct);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, new EventCreateResponse { Id = dto.Id });
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(Guid id, CreateEventDto eventReq, CancellationToken ct)
        {
            await _eventService.UpdateEventAsync(id, eventReq, ct);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _eventService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
