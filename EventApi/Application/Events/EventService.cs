using EventApi.Models.Events;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Application.Events
{
    public class EventService : IEventService
    {
        private readonly ILogger<EventService> _logger;
        private readonly IEventStore _eventStore;

        public EventService(
            IEventStore eventStore,
            ILogger<EventService> logger
            )
        {
            _eventStore = eventStore;
            _logger = logger;
        }

        public Task<PaginatedResult<EventDto>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
        {
            var filtered = _eventStore.GetAll().Select(e => new EventDto(e));

            // Выбрасываем исключение, если pageSize <= 0
            if (pageSize <= 0)
                throw new ValidationException(string.Format("Значение {0} должно быть больше 0.", nameof(pageSize)));

            // Выбрасываем исключение, если page < 1
            if (page < 1)
                throw new ValidationException(string.Format("Значение {0} должно быть больше 0.", nameof(page)));

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

            return Task.FromResult(new PaginatedResult<EventDto>(items, page, curPageSize, totalItems));
        }

        //// Приватный метод для запроса экземпляра сущности из хранилища по id (нужен для Update)
        //private Event GetEventById(Guid id)
        //{
        //    // Добавим для проверки исключение если событие не найдено
        //    if (!_eventStore.Get(id, out var evnt))
        //        throw new EventNotFoundException(string.Format("Событие {0} не найдено.", id));

        //    return evnt;
        //}

        public Task<Event> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            // Добавим для проверки исключение если событие не найдено
            if (!_eventStore.Get(id, out var evnt))
                throw new EventNotFoundException(string.Format("Событие {0} не найдено.", id));

            return Task.FromResult(evnt);
            //return Task.FromResult(new EventDto(GetEventById(id)));
        }

        public Task<EventDto> CreateEventAsync(CreateEventDto dto, CancellationToken ct = default)
        {
            // Валидация инвариантов перенесена в саму модель.
            var evnt = Event.Create(
                title: dto.Title,
                description: dto.Description,
                startAt: dto.StartAt,
                endAt: dto.EndAt,
                totalSeats: dto.TotalSeats,
                withValidation: true
            );

            _eventStore.Add(evnt);

            _logger.LogInformation("Создано событие {EventId}.", evnt.Id);
            return Task.FromResult(new EventDto(evnt));
        }

        public Task<EventDto> UpdateEventAsync(Guid id, CreateEventDto dto, CancellationToken ct = default)
        {
            var evnt = GetEventByIdAsync(id).Result;

            // Валидация инвариантов перенесена в саму модель.
            evnt.Update(
                title: dto.Title,
                description: dto.Description,
                startAt: dto.StartAt,
                endAt: dto.EndAt,
                totalSeats: dto.TotalSeats,
                withValidation: true
            );

            _eventStore.Update(id, evnt);

            _logger.LogInformation("Обновлено событие {EventId}.", evnt.Id);
            return Task.FromResult(new EventDto(evnt));
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var evnt = GetEventByIdAsync(id);

            _eventStore.Remove(id);

            _logger.LogInformation("Удалено событие {EventId}.", evnt.Id);
            return Task.CompletedTask;
        }
    }
}
