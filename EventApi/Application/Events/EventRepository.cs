using EventApi.DataAсcess;
using EventApi.Models.Events;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EventApi.Application.Events
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _dbContext;

        public EventRepository(
            AppDbContext appDbContext
            )
        {
            _dbContext = appDbContext;
        }

        public async Task CreateAsync(Event evnt, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(evnt);

            if (await _dbContext.Events.FindAsync(evnt.Id, ct) != null)
            {
                throw new InvalidOperationException($"Событие с ID {evnt.Id} уже существует.");
            }

            await _dbContext.Events.AddAsync(evnt, ct);
        }

        public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbContext.Events.FindAsync(id, ct);
        }

        public async Task UpdateAsync(Guid id, Event evnt, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(evnt);

            if (await _dbContext.Events.FindAsync(id, ct) == null)
                throw new EventNotFoundException($"Событие {id} не найдено.");

            _dbContext.Events.Update(evnt);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            Event? evnt = await _dbContext.Events.FindAsync(id, ct);
            if (evnt == null)
                throw new EventNotFoundException($"Событие {id} не найдено.");

            _dbContext.Events.Remove(evnt);
        }

        public async Task<IEnumerable<Event>> GetAllAsync(CancellationToken ct = default) =>
            _dbContext.Events
                .AsNoTracking()
                .AsQueryable()
                .OrderBy(e => e.Id);
    }
}