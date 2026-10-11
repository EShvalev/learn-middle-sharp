using EventApi.Models;

namespace EventApi.Application
{
    public interface IEventService
    {
        public List<Event> GetEvents(string? title, DateTime? from, DateTime? to);
        public Event? GetById(int id);
        public int Create(Event evnt);
        public Event? Update(int id, Event newevnt);
        public void Delete(int id);
    }
}
