using System.ComponentModel.DataAnnotations;

namespace EventApi.Models.Events
{
    public class Event
    {
        public Guid Id { get; init; }
        public string Title { get; private set; }
        public string? Description { get; private set; }
        public DateTime? StartAt { get; private set; }
        public DateTime? EndAt { get; private set; }

        private Event() { Title = string.Empty; }

        // Фабричный метод создания/инициализации
        public static Event Create(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null, bool withValidation = true)
        {
            if (withValidation)
                ValidateParams(title, description, startAt, endAt);

            var evnt = new Event()
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt
            };
            return evnt;
        }

        public Event Update(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null, bool withValidation = true)
        {
            ValidateParams(title, description, startAt, endAt);

            this.Title = title;
            this.Description = description;
            this.StartAt = startAt;
            this.EndAt = endAt;

            return this;
        }

        // Валидация доменных инвариантов: если не прошла, выбрасываются исключения
        private static void ValidateParams(string title, string? description, DateTime? startAt, DateTime? endAt)
        {
            if (!(endAt > startAt))
                throw new ValidationException("Дата окончания должна быть позже даты начала события.");
        }
    }
}
