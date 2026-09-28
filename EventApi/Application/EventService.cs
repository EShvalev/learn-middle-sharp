using EventApi.Models;
using System.ComponentModel;

namespace EventApi.Application
{
    public class EventService: IEventService
    {
        private readonly ILogger<EventService> _logger;
        private readonly List<Event> _events = new List<Event>();

        public EventService(ILogger<EventService> logger)
        {
            _logger = logger;
        }

        public List<Event> GetAll() => _events;
        
        public Event? GetById(int id) => _events.FirstOrDefault(e => e.Id == id);
        
        public int Create(Event evnt)
        {
            var maxid = _events.Any() ? _events.Max(e => e.Id) + 1 : 1;

            evnt.Id = maxid;
            _events.Add(evnt);

            _logger.LogInformation($"Создано событие с Id = {evnt.Id}.");
            return evnt.Id;
        }

        public Event? Update(int id, Event newevnt)
        { 
            var evnt = _events.FirstOrDefault(e => e.Id == id);

            if (evnt == null)
            {
                _logger.LogInformation($"Не найдено событие с Id = {id}.");
                return null;
            }

            evnt.Title = newevnt.Title;
            evnt.Description = newevnt.Description;
            evnt.StartAt = newevnt.StartAt;
            evnt.EndAt = newevnt.EndAt;
            return evnt;
        }
        
        public void Delete(int id)
        {
            var evnt = _events.FirstOrDefault(e => e.Id == id);

            if (evnt == null)
            {
                _logger.LogInformation($"Не найдено событие с Id = {id}.");
                return;
            }

            _events.Remove(evnt);
        }
    }
}
