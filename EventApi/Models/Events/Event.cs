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
        private static readonly object _locker = new();

        private Event() { Title = string.Empty; }

        // Фабричный метод создания/инициализации
        public static Event Create(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null, int totalSeats = 0, bool withValidation = true)
        {
            if (withValidation)
                ValidateParams(null, title, description, startAt, endAt, totalSeats);

            var evnt = new Event()
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats
            };
            return evnt;
        }

        public Event Update(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null, int? totalSeats = null, bool withValidation = true)
        {
            // Используем lock, т.к. невозможно гарантировать без критической секции синхронное изменение пары значений TotalSeats и AvailableSeats
            lock (_locker)
            {
                if (withValidation)
                    ValidateParams(this, title, description, startAt, endAt, totalSeats);

                this.Title = title;
                this.Description = description;
                this.StartAt = startAt;
                this.EndAt = endAt;

                if (totalSeats != null && totalSeats != this.TotalSeats)
                {
                    int deltaSeats = totalSeats.Value - this.TotalSeats;
                    this.TotalSeats = totalSeats.Value;
                    this.AvailableSeats += deltaSeats;
                }
            }

            return this;
        }

        // Валидация доменных инвариантов: если не прошла, выбрасываются исключения
        private static void ValidateParams(Event? evnt, string title, string? description, DateTime? startAt, DateTime? endAt, int? totalSeats)
        {
            if (!(endAt > startAt))
                throw new ValidationException("Дата окончания должна быть позже даты начала события.");
            if (totalSeats <= 0)
                throw new ValidationException("Общее количество мест должно быть больше 0.");

            // Следующие проверки выполняются только если событие уже создано (через параметр evnt должен передавиться this либо null если объект еще не создан).
            if (evnt != null)
            {
                // По замечанию из ревью 4, блокируем возможность изменить емкость мест события, если она меньше количества уже сделанных бронирований (только если событие уже создано).
                int reservedSeats = evnt.TotalSeats - evnt.AvailableSeats;
                if (evnt != null && totalSeats < reservedSeats)
                    throw new ValidationException($"Нельзя изменить общее количество мест на значение меньшее, чем уже сделано бронирований ({reservedSeats}).");
            }
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

                if (current + count > this.TotalSeats)
                    return;

                updated = current + count;
            }
            while (Interlocked.CompareExchange(ref this.availableSeats, updated, current) != current);
        }
    }
}
