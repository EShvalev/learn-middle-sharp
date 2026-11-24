using EventApi.Application.Exceptions;
using EventApi.DataAсcess;
using EventApi.Models.Events;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

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
            // Выбрасываем исключение, если pageSize <= 0
            if (pageSize <= 0)
                throw new ValidationException(nameof(pageSize), string.Format("Значение должно быть больше 0."));

            // Выбрасываем исключение, если page < 1
            if (page < 1)
                throw new ValidationException(nameof(page), string.Format("Значение должно быть больше 0."));

            var query = _dbContext.Events
                .AsNoTracking()
                .AsQueryable();

            // По замечанию из ревью 2 спринта, не применяем этот фильтр также если строка пустая
            if (!string.IsNullOrEmpty(title))
                query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

            if (from.HasValue)
                query = query.Where(e => e.StartAt >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.StartAt <= to.Value); // bugfix: была ошибка, проверялось условие по e.EndAt - исправлено (+1 тест + README fixes)

            var items = await query
                .OrderBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var totalCount = await query.CountAsync(ct);

            return new PaginatedResult<EventDto>
            {
                Items = items.Select(e => new EventDto(e)).ToArray(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<EventDto> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            var evnt = await _dbContext.Events.FindAsync(id, ct);
            if (evnt == null)
                throw new NotFoundException($"Событие {id} не найдено.");

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
                    throw new NotFoundException($"Событие {id} не найдено.");

                // Валидация инвариантов перенесена в саму модель.
                evnt.Update(
                    title: dto.Title,
                    description: dto.Description,
                    startAt: dto.StartAt,
                    endAt: dto.EndAt,
                    totalSeats: dto.TotalSeats,
                    withValidation: true
                );

                await _dbContext.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Обновлено событие {EventId}.", id);
            return new EventDto(evnt);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            Event? evnt = await _dbContext.Events.FindAsync(id, ct);
            if (evnt == null)
                throw new NotFoundException($"Событие {id} не найдено.");

            _dbContext.Events.Remove(evnt);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Удалено событие {EventId}.", id);
        }
    }
}
