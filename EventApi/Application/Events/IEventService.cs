using EventApi.Models.Events;

namespace EventApi.Application.Events
{
    public interface IEventService
    {
        public PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize);
        public Event GetById(Guid id);
        public Guid Create(Event evnt);
        public Event Update(Guid id, Event newevnt);
        public void Delete(Guid id);
    }
}
