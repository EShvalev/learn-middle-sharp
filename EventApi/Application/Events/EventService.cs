using EventApi.Models.Events;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Application.Events
{
    public class EventService : IEventService
    {
        private readonly ILogger<EventService> _logger;
        private readonly IEventStore _eventStore;
        //private readonly static List<Event> _events = new List<Event>();

        public EventService(
            IEventStore eventStore,
            ILogger<EventService> logger
            )
        {
            _eventStore = eventStore;
            _logger = logger;
        }

        public PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var filtered = _eventStore.GetAll() as IEnumerable<Event>;

            // Выбрасываем исключение, если pageSize <= 0
            if (pageSize <= 0)
                throw new ValidationException(string.Format("Значение {0} должно быть больше 0.", nameof(pageSize)));

            // По замечанию из ревью 2 спринта, не применяем этот фильтр также если строка пустая
            if (!string.IsNullOrEmpty(title))
                filtered = filtered.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            if (from.HasValue)
                filtered = filtered.Where(e => e.StartAt.HasValue && e.StartAt.Value >= from.Value);

            if (to.HasValue)
                filtered = filtered.Where(e => e.EndAt.HasValue && e.EndAt.Value <= to.Value);

            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            int totalItems = filtered.Count();
            int curPageSize = items.Count();
            //int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PaginatedResult<Event>(items, page, curPageSize, totalItems);
        }

        public Event GetById(Guid id)
        {
            Event? evnt = _eventStore.GetAll().FirstOrDefault(e => e.Id == id);

            // Добавим для проверки исключение если событие не найдено
            if (evnt == null)
                throw new EventNotFoundException(string.Format("Событие {0} не найдено.", id));

            return evnt;
        }

        public Guid Create(Event evnt)
        {
            // Эта валидация с выбросом исключения добавлена только для задания тестирования 2 спринта.
            if (!(evnt.EndAt > evnt.StartAt))
            {
                throw new ValidationException("Дата окончания должна быть позже даты начала события.");
            }

            //var maxid = _events.Any() ? _events.Max(e => e.Id) + 1 : 1;

            _eventStore.Add(evnt);

            _logger.LogInformation("Создано событие с Id = {0}.", evnt.Id);
            return evnt.Id;
        }

        public Event Update(Guid id, Event newevnt)
        {
            // Эта валидация с выбросом исключения добавлена только для задания тестирования 2 спринта.
            if (!(newevnt.EndAt > newevnt.StartAt))
            {
                throw new ValidationException("Дата окончания должна быть позже даты начала события.");
            }

            var evnt = GetById(id);

            evnt.Title = newevnt.Title;
            evnt.Description = newevnt.Description;
            evnt.StartAt = newevnt.StartAt;
            evnt.EndAt = newevnt.EndAt;

            _logger.LogInformation("Обновлено событие с Id = {0}.", evnt.Id);
            return evnt;
        }

        public void Delete(Guid id)
        {
            var evnt = GetById(id);

            _eventStore.Remove(id);
            _logger.LogInformation("Удалено событие с Id = {0}.", evnt.Id);
        }
    }
}
