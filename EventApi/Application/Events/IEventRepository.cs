using EventApi.Models.Events;

namespace EventApi.Application.Events
{
    public interface IEventRepository
    {
        public Task CreateAsync(Event evnt, CancellationToken ct);
        public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);
        public Task UpdateAsync(Guid id, Event evnt, CancellationToken ct);
        public Task DeleteAsync(Guid id, CancellationToken ct);
        public Task<IEnumerable<Event>> GetAllAsync(CancellationToken ct);
    }
}
