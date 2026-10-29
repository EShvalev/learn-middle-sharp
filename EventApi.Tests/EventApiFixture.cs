using EventApi.Application.Bookings;
using EventApi.Application.Events;
using EventApi.Models.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventApi.Tests
{
    //[CollectionDefinition("GlobalCollection")]
    //public class GlobalCollection : ICollectionFixture<EventApiFixture> { }

    public class EventApiFixture
    {
        public IBookingStore bookingStore { get; set; }
        public IBookingService bookingService { get; set; }
        public IEventStore eventStore { get; set; }
        public IEventService eventService { get; set; }
        private readonly List<Event> _initStore = [
            new Event() { Id = Guid.Parse("00000001-0000-0000-0000-000000000000"), Title = "Джем-сэшн", Description = "Константин Хазанович", StartAt = Convert.ToDateTime("2026-10-06T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-06T21:00:00+03")},
            new Event() { Id = Guid.Parse("00000002-0000-0000-0000-000000000000"), Title = "Романтический джаз", Description = "Эльвира Трафова", StartAt = Convert.ToDateTime("2026-10-07T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-07T21:00:00+03")},
            new Event() { Id = Guid.Parse("00000003-0000-0000-0000-000000000000"), Title = "Петербургские мотивы", Description = "Ильдар Казаханов и его бэнд", StartAt = Convert.ToDateTime("2026-10-08T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-08T21:00:00+03")},
            new Event() { Id = Guid.Parse("00000004-0000-0000-0000-000000000000"), Title = "Джазовые портреты вокалистов", Description = "Звезды мирового джаза", StartAt = Convert.ToDateTime("2026-10-09T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-09T21:00:00+03")},
            new Event() { Id = Guid.Parse("00000005-0000-0000-0000-000000000000"), Title = "Сказки Чижика", Description = "Цикл джазовых сказок для самых маленьких", StartAt = Convert.ToDateTime("2026-10-10T12:00:00+03"), EndAt = Convert.ToDateTime("2026-10-10T14:00:00+03")},
            new Event() { Id = Guid.Parse("00000006-0000-0000-0000-000000000000"), Title = "Вечер памяти Станислава Говорухина", Description = "", StartAt = Convert.ToDateTime("2026-10-11T18:00:00+03"), EndAt = Convert.ToDateTime("2026-10-11T20:30:00+03")},
            new Event() { Id = Guid.Parse("00000007-0000-0000-0000-000000000000"), Title = "Весь этот джаз", Description = "Денис Кузьмин и ансамбль", StartAt = Convert.ToDateTime("2026-10-12T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-12T21:00:00+03")},
            new Event() { Id = Guid.Parse("00000008-0000-0000-0000-000000000000"), Title = "Джем-сэшн", Description = "Константин Хазанович", StartAt = Convert.ToDateTime("2026-10-20T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-20T21:00:00+03")},
            new Event() { Id = Guid.Parse("00000009-0000-0000-0000-000000000000"), Title = "Лэтин джаз", Description = "Концерт программы", StartAt = Convert.ToDateTime("2026-10-22T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-22T21:00:00+03")},
            new Event() { Id = Guid.Parse("0000000a-0000-0000-0000-000000000000"), Title = "Традиционный джаз и свинг", Description = "", StartAt = Convert.ToDateTime("2026-10-24T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-24T21:00:00+03")},
            new Event() { Id = Guid.Parse("0000000b-0000-0000-0000-000000000000"), Title = "Губная гармоника в джазе", Description = "", StartAt = Convert.ToDateTime("2026-10-27T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-27T21:00:00+03")},
            new Event() { Id = Guid.Parse("0000000c-0000-0000-0000-000000000000"), Title = "Трибьют Фрэнку Синатре", Description = "", StartAt = Convert.ToDateTime("2026-10-30T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-30T21:00:00+03")}
            ];

        public EventApiFixture()
        {
            eventStore = new EventStore();

            eventService = new EventService(eventStore, NullLogger<EventService>.Instance);

            bookingStore = new InMemoryBookingStore();

            bookingService = new BookingService(eventService, bookingStore, NullLogger<BookingService>.Instance);

            // Предзаполним список события в сервисе для тестирования выборок и пагинаций
            InitEvents(eventService);

            Console.WriteLine("EventApiFixture() constructor called.");
        }

        private void InitEvents(IEventService eventService)
        {
            foreach (var evnt in _initStore)
                eventService.Create(evnt);

            Console.WriteLine("EventApiFixture.InitEvents() called.");
        }
    }
}
