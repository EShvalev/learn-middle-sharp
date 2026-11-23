using EventApi.Application;
using EventApi.Application.Bookings;
using EventApi.Application.Events;
using EventApi.DataAсcess;
using EventApi.Models.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace EventApi.Tests
{
    //[CollectionDefinition("GlobalCollection")]
    //public class GlobalCollection : ICollectionFixture<EventApiFixture> { }

    public class EventApiFixture
    {
        public IServiceProvider serviceProvider { get; set; }

        public Guid zeroGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
        public Guid testEventGuid { get; set; }
        public Guid testEventGuidToDelete { get; set; }


        private readonly List<CreateEventDto> _initStore = [
            new CreateEventDto() { Title = "Джем-сэшн", Description = "Константин Хазанович", StartAt = Convert.ToDateTime("2026-10-06T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-06T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Романтический джаз", Description = "Эльвира Трафова", StartAt = Convert.ToDateTime("2026-10-07T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-07T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Петербургские мотивы", Description = "Ильдар Казаханов и его бэнд", StartAt = Convert.ToDateTime("2026-10-08T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-08T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Джазовые портреты вокалистов", Description = "Звезды мирового джаза", StartAt = Convert.ToDateTime("2026-10-09T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-09T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Сказки Чижика", Description = "Цикл джазовых сказок для самых маленьких", StartAt = Convert.ToDateTime("2026-10-10T12:00:00+03"), EndAt = Convert.ToDateTime("2026-10-10T14:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Вечер памяти Станислава Говорухина", Description = "", StartAt = Convert.ToDateTime("2026-10-11T18:00:00+03"), EndAt = Convert.ToDateTime("2026-10-11T20:30:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Весь этот джаз", Description = "Денис Кузьмин и ансамбль", StartAt = Convert.ToDateTime("2026-10-12T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-12T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Джем-сэшн", Description = "Константин Хазанович", StartAt = Convert.ToDateTime("2026-10-20T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-20T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Лэтин джаз", Description = "Концерт программы", StartAt = Convert.ToDateTime("2026-10-22T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-22T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Традиционный джаз и свинг", Description = "", StartAt = Convert.ToDateTime("2026-10-24T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-24T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Губная гармоника в джазе", Description = "", StartAt = Convert.ToDateTime("2026-10-27T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-27T21:00:00+03"), TotalSeats = 20},
            new CreateEventDto() { Title = "Трибьют Фрэнку Синатре", Description = "", StartAt = Convert.ToDateTime("2026-10-30T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-30T21:00:00+03"), TotalSeats = 20}
            ];

        public EventApiFixture()
        {
            // Регистрируем коллекцию сервисов
            Console.OutputEncoding = Encoding.UTF8;
            var services = new ServiceCollection().AddLogging(builder =>
            {
                builder.AddConsole(); // Добавляем вывод в консоль
                builder.SetMinimumLevel(LogLevel.Information); // Минимальный уровень
            });

            // БД контекст (отдельная БД для каждого тестового класса, обеспечивается уникальным именем)
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Собственные сервисы, взаимодействие которых тестируем
            services.AddSingleton<IKeyedLockManager, KeyedLockManager>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddHostedService<BookingProcessingService>();

            // Создаем сервис провайдер
            serviceProvider = services.BuildServiceProvider();

            // Запрашиваем EventService для создания тестовой коллекции событий
            var eventService = serviceProvider.GetRequiredService<IEventService>();
            // Предзаполним список события в сервисе для тестирования выборок и пагинаций
            InitEvents(eventService);

            // Ищем Id выбранных события для тестов, когда событие должно быть для штатного выполнения (testEventGuid),
            // и для тестов на удаление события или связанные действия если событие удалено (testEventGuidToDelete).
            testEventGuid = GetEventIdByTitle(eventService, "Романтический джаз");
            testEventGuidToDelete = GetEventIdByTitle(eventService, "Джем-сэшн");

            Console.WriteLine("EventApiFixture() constructor called.");
        }

        private void InitEvents(IEventService eventService)
        {
            foreach (var evnt in _initStore)
                eventService.CreateEventAsync(evnt, CancellationToken.None);

            Console.WriteLine("EventApiFixture.InitEvents() called.");
        }

        private Guid GetEventIdByTitle(IEventService eventService, string title)
        {
            var events = eventService.GetEventsAsync(title, null, null, 1, 1000000, CancellationToken.None).Result;
            var evnt = events.Items.FirstOrDefault(e => e.Title == title);
            return evnt != null ? evnt.Id : zeroGuid;
        }
    }
}
