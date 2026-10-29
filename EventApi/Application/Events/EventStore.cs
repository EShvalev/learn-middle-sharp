using EventApi.Models.Events;
using System.Collections.Concurrent;

namespace EventApi.Application.Events
{
    public class EventStore : IEventStore
    {
        private readonly ConcurrentDictionary<Guid, Event> _dict = new ConcurrentDictionary<Guid, Event>();

        public void Add(Event evnt)
        {
            _dict[evnt.Id] = evnt;
        }

        public bool Get(Guid id, out Event evnt)
        {
            return _dict.TryGetValue(id, out evnt);
        }

        public bool Remove(Guid id)
        {
            return _dict.TryRemove(id, out var _);
        }

        public bool Update(Guid id, Event evnt)
        {
            return Get(id, out var orig_evnt) ? _dict.TryUpdate(id, evnt, orig_evnt) : false;
        }

        public IEnumerable<Event> GetAll() => _dict.Values.OrderBy(e => e.Id);
    }
}
