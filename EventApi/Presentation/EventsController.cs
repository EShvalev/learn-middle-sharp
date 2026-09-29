using EventApi.Application;
using EventApi.Models;
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
        public IActionResult GetAll()
        { 
            return Ok(_eventService.GetAll());
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var evnt = _eventService.GetById(id);
            return evnt != null ? Ok(evnt) : NotFound();
        }

        [HttpPost()]
        public IActionResult Create([FromBody] EventDto eventdto)
        {
            if (eventdto.EndAt <= eventdto.StartAt)
            {
                ModelState.AddModelError("Дата окончания", "Дата окончания должна быть позже даты начала события.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var evnt = new Event { 
                Title = eventdto.Title,
                Description = eventdto.Description,
                StartAt = eventdto.StartAt,
                EndAt = eventdto.EndAt
                };
            var id = _eventService.Create(evnt);
            
            return CreatedAtAction(nameof(Create), new { id = evnt.Id }, evnt);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, EventDto eventdto)
        {
            if (eventdto.EndAt <= eventdto.StartAt)
            {
                ModelState.AddModelError("Дата окончания", "Дата окончания должна быть позже даты начала события.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
        public IActionResult Delete(int id)
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
