using EventApi.Models.Events;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

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

        // Приватный метод для запроса экземпляра сущности из хранилища по id (нужен для Update)
        private Event GetEventById(Guid id)
        {
            Event? evnt = _eventStore.GetAll().FirstOrDefault(e => e.Id == id);

            // Добавим для проверки исключение если событие не найдено
            if (evnt == null)
                throw new EventNotFoundException(string.Format("Событие {0} не найдено.", id));

            return evnt;
        }

        public Task<EventDto> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(new EventDto(GetEventById(id)));
        }

        public Task<EventDto> CreateEventAsync(CreateEventDto dto, CancellationToken ct = default)
        {
            // Эта валидация с выбросом исключения добавлена для тестов (проверка невалидных данных без использования контроллера).
            dto.ThrowsExceptionIfInvalid();

            var evnt = Event.CreateNew(
                title: dto.Title,
                description: dto.Description,
                startAt: dto.StartAt,
                endAt: dto.EndAt
            );

            _eventStore.Add(evnt);

            _logger.LogInformation("Создано событие с Id = {0}.", evnt.Id);
            return Task.FromResult(new EventDto(evnt));
        }

        // Метод нужен для тестов (первичное заполнение хранилища событий с предопределенными Id, без валидации).
        public Guid CreateForTests(Event evnt)
        {
            _eventStore.Add(evnt);

            _logger.LogInformation("Создано событие с Id = {0}.", evnt.Id);
            return evnt.Id;
        }

        public Task<EventDto> UpdateEventAsync(Guid id, CreateEventDto dto, CancellationToken ct = default)
        {
            // Эта валидация с выбросом исключения добавлена для тестов (проверка невалидных данных без использования контроллера).
            dto.ThrowsExceptionIfInvalid();

            var evnt = GetEventById(id);

            evnt.Title = dto.Title;
            evnt.Description = dto.Description;
            evnt.StartAt = dto.StartAt;
            evnt.EndAt = dto.EndAt;

            _logger.LogInformation("Обновлено событие с Id = {0}.", evnt.Id);
            return Task.FromResult(new EventDto(evnt));
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var evnt = GetEventByIdAsync(id);

            _eventStore.Remove(id);
            
            _logger.LogInformation("Удалено событие с Id = {0}.", evnt.Id);
            return Task.CompletedTask;
        }
    }
}
