using EventApi.Models.Events;
using System.Collections.Concurrent;

namespace EventApi.Application.Events
{
    public class EventStore : IEventStore
    {
        private readonly ConcurrentDictionary<Guid, Event> _events = new ConcurrentDictionary<Guid, Event>();

        public async Task CreateAsync(Event evnt, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(evnt);

            if (!_events.TryAdd(evnt.Id, evnt.Clone()))
            {
                throw new InvalidOperationException($"Событие с ID {evnt.Id} уже существует.");
            }
        }

        public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _events.TryGetValue(id, out var evnt) ? evnt.Clone() : null;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            if (!_events.TryGetValue(id, out var evnt))
                throw new EventNotFoundException($"Событие {id} не найдено.");
            return _events.TryRemove(id, out var _);
        }

        public async Task UpdateAsync(Guid id, Event evnt, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(evnt);

            if (!_events.TryGetValue(id, out var _))
                throw new EventNotFoundException($"Событие {id} не найдено.");

            _events[id] = evnt.Clone();
        }

        public async Task<IEnumerable<Event>> GetAllAsync(CancellationToken ct = default) => _events.Values.Select(e => e.Clone()).OrderBy(e => e.Id);
    }
}
