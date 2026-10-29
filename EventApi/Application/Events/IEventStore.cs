using EventApi.Models.Events;

namespace EventApi.Application.Events
{
    public interface IEventStore
    {
        public void Add(Event evnt);
        public bool Get(Guid id, out Event evnt);
        public bool Remove(Guid id);
        public bool Update(Guid id, Event evnt);
        public IEnumerable<Event> GetAll();
    }
}
