using EventApi.Models.Events;

namespace EventApi.Application.Events
{
    public interface IEventService
    {
        public Task<PaginatedResult<EventDto>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
        public Task<EventDto> GetEventByIdAsync(Guid id, CancellationToken ct);
        public Task<EventDto> CreateEventAsync(CreateEventDto dto, CancellationToken ct);
        public Guid CreateForTests(Event evnt);
        public Task<EventDto> UpdateEventAsync(Guid id, CreateEventDto dto, CancellationToken ct);
        public Task DeleteAsync(Guid id, CancellationToken ct);
    }
}
