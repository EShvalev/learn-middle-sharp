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
        public int TotalSeats { get; private set; }
        private int availableSeats;
        public int AvailableSeats { get => availableSeats; private set => availableSeats = value; }

        private Event() { Title = string.Empty; }

        // Фабричный метод создания/инициализации
        public static Event Create(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null, int? totalSeats = 1, bool withValidation = true)
        {
            if (withValidation)
                ValidateParams(title, description, startAt, endAt, totalSeats);

            var evnt = new Event()
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats ?? 1,
                AvailableSeats = totalSeats ?? 1
            };
            return evnt;
        }

        public Event Update(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null, int? totalSeats = null, bool withValidation = true)
        {
            if (withValidation)
                ValidateParams(title, description, startAt, endAt, totalSeats);

            this.Title = title;
            this.Description = description;
            this.StartAt = startAt;
            this.EndAt = endAt;

            if (totalSeats != null && totalSeats != this.TotalSeats)
            {
                this.TotalSeats = totalSeats.Value;
                this.AvailableSeats = totalSeats.Value;
            }

            return this;
        }

        // Валидация доменных инвариантов: если не прошла, выбрасываются исключения
        private static void ValidateParams(string title, string? description, DateTime? startAt, DateTime? endAt, int? totalSeats)
        {
            if (!(endAt > startAt))
                throw new ValidationException("Дата окончания должна быть позже даты начала события.");
            if (totalSeats <= 0)
                throw new ValidationException("Общее количество мест должно быть больше 0.");
        }

        public bool TryReserveSeats(int count = 1)
        {
            int current, updated;

            do
            {
                current = this.AvailableSeats;

                if (current - count < 0)
                    return false;

                updated = current - count;
            }
            while (Interlocked.CompareExchange(ref this.availableSeats, updated, current) != current);

            return true;
        }
        public void ReleaseSeats(int count = 1)
        {
            int current, updated;

            do
            {
                current = this.AvailableSeats;
                updated = current + count;
            }
            while (Interlocked.CompareExchange(ref this.availableSeats, updated, current) != current);
        }
    }
}
