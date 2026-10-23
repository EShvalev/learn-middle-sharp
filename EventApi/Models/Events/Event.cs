using EventApi.Models.Bookings;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Models.Events
{
    public class Event
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        // Фабричный метод создания/инициализации
        public static Event CreateNew(string title, string? description = null, DateTime? startAt = null, DateTime? endAt = null)
        {
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
    }
}
