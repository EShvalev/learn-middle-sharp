using EventApi.Models.Bookings;
using EventApi.Application.Exceptions;

namespace EventApi.Models.Events
{
    public class Event
    {
        public Guid Id { get; init; }
        public string Title { get; private set; } = null!;
        public string? Description { get; private set; }
        public DateTime StartAt { get; private set; }
        public DateTime EndAt { get; private set; }
        public int TotalSeats { get; private set; }
        public int AvailableSeats { get; private set; }
        public List<Booking> Bookings { get; private set; } = null!;

        private Event() { }

        // Фабричный метод создания/инициализации
        public static Event Create(string title, string? description, DateTime? startAt, DateTime? endAt, int? totalSeats, bool withValidation = true)
        {
            if (withValidation)
                ValidateParams(null, title, description, startAt, endAt, totalSeats);

            var evnt = new Event()
            {
                Id = Guid.NewGuid(),
                Title = title!.Trim(),
                Description = description,
                StartAt = startAt!.Value,
                EndAt = endAt!.Value,
                TotalSeats = totalSeats!.Value,
                AvailableSeats = totalSeats!.Value
            };
            return evnt;
        }

        // Метод Clone(), используется в Get методах для отказа от прямого доступа к элементам хранилища (подготовка к переходу на EF)
        public Event Clone() => new()
        {
            Id = this.Id,
            Title = this.Title,
            Description = this.Description,
            StartAt = this.StartAt,
            EndAt = this.EndAt,
            TotalSeats = this.TotalSeats,
            AvailableSeats = this.AvailableSeats
        };

        public Event Update(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null, int? totalSeats = null, bool withValidation = true)
        {
            if (withValidation)
                ValidateParams(this, title, description, startAt, endAt, totalSeats);

            this.Title = title;
            this.Description = description;
            this.StartAt = startAt!.Value;
            this.EndAt = endAt!.Value;

            if (totalSeats != null && totalSeats != this.TotalSeats)
            {
                int deltaSeats = totalSeats.Value - this.TotalSeats;
                this.TotalSeats = ResetSeats(0, int.MaxValue, totalSeats.Value);
                this.AvailableSeats = ResetSeats(0, this.TotalSeats, this.AvailableSeats + deltaSeats);
            }

            return this;
        }

        // Валидация доменных инвариантов: если не прошла, выбрасываются исключения
        private static void ValidateParams(Event? evnt, string title, string? description, DateTime? startAt, DateTime? endAt, int? totalSeats)
        {
            var errors = new Dictionary<string, ICollection<string>>();

            if (string.IsNullOrWhiteSpace(title))
                AddError(errors, nameof(Title), "Заголовок события обязателен.");

            if (!startAt.HasValue)
                AddError(errors, nameof(StartAt), "Дата начала события обязательна.");

            if (!endAt.HasValue)
                AddError(errors, nameof(EndAt), "Дата окончания события обязательна.");

            if (endAt <= startAt)
                AddError(errors, nameof(EndAt), "Дата окончания должна быть позже даты начала события.");

            if (!totalSeats.HasValue || totalSeats.Value <= 0)
                AddError(errors, nameof(TotalSeats), "Общее количество мест должно быть больше 0.");

            // Следующие проверки выполняются только если событие еще не создано.
            if (evnt == null)
            {
                if (!totalSeats.HasValue)
                    AddError(errors, nameof(TotalSeats), "Общее количество мест должно быть задано при создании события.");
            }

            // Следующие проверки выполняются только если событие уже создано (через параметр evnt должен передаваться this либо null если объект еще не создан).
            if (evnt != null)
            {
                // По замечанию из ревью 4, блокируем возможность изменить емкость мест события, если она меньше количества уже сделанных бронирований (только если событие уже создано).
                int reservedSeats = evnt.TotalSeats - evnt.AvailableSeats;
                if (evnt != null && totalSeats < reservedSeats)
                    AddError(errors, nameof(TotalSeats), $"Нельзя изменить общее количество мест на значение меньшее, чем уже сделано бронирований ({reservedSeats}).");
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private static void AddError(Dictionary<string, ICollection<string>> errors, string field, string message)
        {
            if (!errors.ContainsKey(field))
                errors[field] = new List<string>();

            errors[field].Add(message);
        }

        private int ResetSeats(int minSeats, int maxSeats, int updatedSeats)
        {
            // 19.11.2026: после добавления KeyLockedManager как сквозного сервиса блокировки убрал CAS вызовы по замечанию "не стоит смешивать два типа блокировок на изменении одного объекта" 
            // Оставил закомментрованную прежнюю версию до следующего спринта.
            //int current, updated;

            //do
            //{
            //    current = storeSeats;

            //    if (updatedSeats < minSeats)
            //        updated = minSeats;
            //    else if (updatedSeats > maxSeats)
            //        updated = maxSeats;
            //    else
            //        updated = updatedSeats;
            //}
            //while (Interlocked.CompareExchange(ref storeSeats, updated, current) != current);

            return Math.Max(minSeats, Math.Min(maxSeats, updatedSeats));
        }

        public bool TryReserveSeats(int count = 1)
        {
            // 19.11.2026: после добавления KeyLockedManager как сквозного сервиса блокировки убрал CAS вызовы по замечанию "не стоит смешивать два типа блокировок на изменении одного объекта" 
            // Оставил закомментрованную прежнюю версию до следующего спринта.
            //int current, updated;

            //do
            //{
            //    current = this.AvailableSeats;

            //    if (current - count < 0)
            //        return false;

            //    updated = current - count;
            //}
            //while (Interlocked.CompareExchange(ref this.availableSeats, updated, current) != current);

            if (this.AvailableSeats < count)
                return false;

            this.AvailableSeats -= count;

            return true;
        }

        public void ReleaseSeats(int count = 1)
        {
            // 19.11.2026: после добавления KeyLockedManager как сквозного сервиса блокировки убрал CAS вызовы по замечанию "не стоит смешивать два типа блокировок на изменении одного объекта" 
            // Оставил закомментрованную прежнюю версию до следующего спринта.
            //int current, updated;

            //do
            //{
            //    current = this.AvailableSeats;

            //    if (current + count > this.TotalSeats)
            //        return;

            //    updated = current + count;
            //}
            //while (Interlocked.CompareExchange(ref this.availableSeats, updated, current) != current);

            this.AvailableSeats = Math.Max(0, Math.Min(TotalSeats, AvailableSeats + count));
        }
    }
}
