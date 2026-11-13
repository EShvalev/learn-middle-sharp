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
        private int totalSeats;
        public int TotalSeats { get => totalSeats; private set => totalSeats = value; }
        private int availableSeats;
        public int AvailableSeats { get => availableSeats; private set => availableSeats = value; }

        private Event() { Title = string.Empty; }

        // Фабричный метод создания/инициализации
        public static Event Create(string title, string? description, DateTime? startAt, DateTime? endAt, int? totalSeats, bool withValidation = true)
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
                TotalSeats = totalSeats.Value, // totalSeats здесь не может быть null, т.к. ValidateParams() раньше выбросит исключение
                AvailableSeats = totalSeats.Value
            };
            return evnt;
        }

        public Event Update(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null, int? totalSeats = null, bool withValidation = true)
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
                TryResetSeats(ref this.totalSeats, 0, int.MaxValue, totalSeats.Value);
                TryResetSeats(ref this.availableSeats, 0, this.TotalSeats, this.AvailableSeats + deltaSeats);
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

            // Следующие проверки выполняются только если событие еще не создано.
            if (evnt == null)
            {
                if (totalSeats == null)
                    throw new ValidationException("Общее количество мест должно быть задано.");
            }

            // Следующие проверки выполняются только если событие уже создано (через параметр evnt должен передаваться this либо null если объект еще не создан).
            if (evnt != null)
            {
                // По замечанию из ревью 4, блокируем возможность изменить емкость мест события, если она меньше количества уже сделанных бронирований (только если событие уже создано).
                int reservedSeats = evnt.TotalSeats - evnt.AvailableSeats;
                if (evnt != null && totalSeats < reservedSeats)
                    throw new ValidationException($"Нельзя изменить общее количество мест на значение меньшее, чем уже сделано бронирований ({reservedSeats}).");
            }
        }

        private void TryResetSeats(ref int storeSeats, int minSeats, int maxSeats, int updatedSeats)
        {
            int current, updated;

            do
            {
                current = storeSeats;

                if (updatedSeats < minSeats)
                    updated = minSeats;
                else if (updatedSeats > maxSeats)
                    updated = maxSeats;
                else
                    updated = updatedSeats;
            }
            while (Interlocked.CompareExchange(ref storeSeats, updated, current) != current);
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
