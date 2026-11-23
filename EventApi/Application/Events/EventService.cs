using EventApi.DataAсcess;
using EventApi.Models.Events;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Application.Events
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _dbContext;
        private readonly IKeyedLockManager _lockManager;
        private readonly ILogger<EventService> _logger;

        public EventService(
            AppDbContext dbContext,
            IKeyedLockManager lockManager,
            ILogger<EventService> logger
            )
        {
            _dbContext = dbContext;
            _lockManager = lockManager;
            _logger = logger;
        }

        public async Task<PaginatedResult<EventDto>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
        {
            var filtered = _dbContext.Events
                .AsNoTracking()
                .AsQueryable();

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

            var items = await filtered
                .OrderBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EventDto(e))
                .ToListAsync();

            int totalItems = filtered.Count();
            int curPageSize = items.Count();

            return new PaginatedResult<EventDto>(items, page, curPageSize, totalItems);
        }

        public async Task<EventDto> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            var evnt = await _dbContext.Events.FindAsync(id, ct);
            if (evnt == null)
                throw new EventNotFoundException($"Событие {id} не найдено.");

            return new EventDto(evnt);
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto dto, CancellationToken ct = default)
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

            await _dbContext.Events.AddAsync(evnt, ct);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Создано событие {EventId}.", evnt.Id);
            return new EventDto(evnt);
        }

        public async Task<EventDto> UpdateEventAsync(Guid id, CreateEventDto dto, CancellationToken ct = default)
        {
            Event? evnt;

            using (await _lockManager.AcquireEventLockAsync(id, ct))
            {
                evnt = await _dbContext.Events.FindAsync(id, ct);
                if (evnt == null)
                    throw new EventNotFoundException($"Событие {id} не найдено.");

                // Валидация инвариантов перенесена в саму модель.
                evnt.Update(
                    title: dto.Title,
                    description: dto.Description,
                    startAt: dto.StartAt,
                    endAt: dto.EndAt,
                    totalSeats: dto.TotalSeats,
                    withValidation: true
                );

                _dbContext.Events.Update(evnt);
                await _dbContext.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Обновлено событие {EventId}.", id);
            return new EventDto(evnt);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            Event? evnt = await _dbContext.Events.FindAsync(id, ct);
            if (evnt == null)
                throw new EventNotFoundException($"Событие {id} не найдено.");

            _dbContext.Events.Remove(evnt);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Удалено событие {EventId}.", id);
        }
    }
}
