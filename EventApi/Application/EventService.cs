using EventApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Application
{
    public class EventService : IEventService
    {
        private readonly ILogger<EventService> _logger;
        private readonly static List<Event> _events = new List<Event>();

        public EventService(ILogger<EventService> logger)
        {
            _logger = logger;
        }

        public PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var filtered = _events as IEnumerable<Event>;

            // Выбрасываем исключение, если pageSize <= 0
            if (pageSize <= 0)
                throw new ValidationException("Значение pageSize должно быть больше 0.");

            if (title != null)
                filtered = filtered.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            if (from.HasValue)
                filtered = filtered.Where(e => e.StartAt.HasValue && e.StartAt.Value >= from.Value);

            if (to.HasValue)
                filtered = filtered.Where(e => e.EndAt.HasValue && e.EndAt.Value <= to.Value);

            filtered = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            int totalCount = filtered.Count();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PaginatedResult<Event>(filtered, page, totalPages, totalCount);
        }            

        public Event? GetById(int id)
        {
            Event? evnt = _events.FirstOrDefault(e => e.Id == id);

            // Добавим для проверки исключение если событие не найдено
            if (evnt == null)
                throw new EventNotFoundException(string.Format("Событие {0} не найдено.", id));

            return evnt;
        }

        public int Create(Event evnt)
        {
            var maxid = _events.Any() ? _events.Max(e => e.Id) + 1 : 1;

            evnt.Id = maxid;
            _events.Add(evnt);

            _logger.LogInformation("Создано событие с Id = {0}.", evnt.Id);
            return evnt.Id;
        }

        public Event? Update(int id, Event newevnt)
        {
            var evnt = GetById(id);

            if (evnt == null)
            {
                _logger.LogInformation("Не найдено событие с Id = {0}.", id);
                return null;
            }

            evnt.Title = newevnt.Title;
            evnt.Description = newevnt.Description;
            evnt.StartAt = newevnt.StartAt;
            evnt.EndAt = newevnt.EndAt;

            _logger.LogInformation("Обновлено событие с Id = {0}.", evnt.Id);
            return evnt;
        }

        public void Delete(int id)
        {
            var evnt = GetById(id);

            if (evnt == null)
            {
                _logger.LogInformation("Не найдено событие с Id = {0}.", id);
                return;
            }

            _logger.LogInformation("Удалено событие с Id = {0}.", evnt.Id);
            _events.Remove(evnt);
        }
    }
}
