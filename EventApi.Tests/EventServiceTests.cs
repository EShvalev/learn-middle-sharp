using EventApi.Application;
using EventApi.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel.DataAnnotations;
using Xunit.Extensions.Ordering;

// Регистрация глобальных упорядочивателей для классов и коллекций
[assembly: TestCollectionOrderer("Xunit.Extensions.Ordering.CollectionOrderer", "Xunit.Extensions.Ordering")]
[assembly: TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]

namespace EventApi.Tests
{
    [Order(1)]
    public class EventServiceFixture
    {
        public EventService eventService { get; set; }
        private readonly List<Event> _initStore = [
            new Event() { Id = 1, Title = "Джем-сэшн", Description = "Константин Хазанович", StartAt = Convert.ToDateTime("2026-10-06T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-06T21:00:00+03")},
            new Event() { Id = 2, Title = "Романтический джаз", Description = "Эльвира Трафова", StartAt = Convert.ToDateTime("2026-10-07T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-07T21:00:00+03")},
            new Event() { Id = 3, Title = "Петербургские мотивы", Description = "Ильдар Казаханов и его бэнд", StartAt = Convert.ToDateTime("2026-10-08T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-08T21:00:00+03")},
            new Event() { Id = 4, Title = "Джазовые портреты вокалистов", Description = "Звезды мирового джаза", StartAt = Convert.ToDateTime("2026-10-09T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-09T21:00:00+03")},
            new Event() { Id = 5, Title = "Сказки Чижика", Description = "Цикл джазовых сказок для самых маленьких", StartAt = Convert.ToDateTime("2026-10-10T12:00:00+03"), EndAt = Convert.ToDateTime("2026-10-10T14:00:00+03")},
            new Event() { Id = 6, Title = "Вечер памяти Станислава Говорухина", Description = "", StartAt = Convert.ToDateTime("2026-10-11T18:00:00+03"), EndAt = Convert.ToDateTime("2026-10-11T20:30:00+03")},
            new Event() { Id = 7, Title = "Весь этот джаз", Description = "Денис Кузьмин и ансамбль", StartAt = Convert.ToDateTime("2026-10-12T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-12T21:00:00+03")},
            new Event() { Id = 8, Title = "Джем-сэшн", Description = "Константин Хазанович", StartAt = Convert.ToDateTime("2026-10-20T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-20T21:00:00+03")},
            new Event() { Id = 9, Title = "Лэтин джаз", Description = "Концерт программы", StartAt = Convert.ToDateTime("2026-10-22T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-22T21:00:00+03")},
            new Event() { Id = 10, Title = "Традиционный джаз и свинг", Description = "", StartAt = Convert.ToDateTime("2026-10-24T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-24T21:00:00+03")},
            new Event() { Id = 11, Title = "Губная гармоника в джазе", Description = "", StartAt = Convert.ToDateTime("2026-10-27T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-27T21:00:00+03")},
            new Event() { Id = 12, Title = "Трибьют Фрэнку Синатре", Description = "", StartAt = Convert.ToDateTime("2026-10-30T19:00:00+03"), EndAt = Convert.ToDateTime("2026-10-30T21:00:00+03")}
            ];

        public EventServiceFixture()
        {
            ILogger<EventService> logger = NullLogger<EventService>.Instance;

            eventService = new EventService(logger);

            // Предзаполним список события в сервисе для тестирования выборок и пагинаций
            InitEvents(eventService);
        }

        private void InitEvents(EventService eventService)
        {
            foreach (var evnt in _initStore)
                eventService.Create(evnt);
        }
    }

    public class EventServiceTests : IClassFixture<EventServiceFixture>
    {
        private readonly EventService _eventService;
        private readonly int _VeryLongPageSize = 1000000;

        public EventServiceTests(EventServiceFixture fixture)
        {
            _eventService = fixture.eventService;
        }

        // Вспомогательный метод: возвращает Id первого события в списке (используется в успешных сценариях поиска/изменения/удаления по Id).
        private int GetFirstExistingEventId()
        {
            var result = _eventService.GetEvents(null, null, null, 1, _VeryLongPageSize);
            return result.TotalItems > 0 ? result.Items.FirstOrDefault(new Event { Id = 0 }).Id : 0;
        }

        [Fact]
        [Order(2)]
        public void CreateEvent_OneEvent_ReturnsId()
        {
            // Arrange
            var evnt = new Event() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };

            // Act
            var id = _eventService.Create(evnt);

            // Assert
            Assert.True(id > 0);
        }

        [Fact]
        [Order(2)]
        public void CreateEvent_IfDateInvalid_ThrowsValidationExeption()
        {
            // Arrange
            var evnt = new Event() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-05"), EndAt = Convert.ToDateTime("2026-07-02") };

            // Act & Assert
            Assert.Throws<ValidationException>(() => _eventService.Create(evnt));
        }

        [Fact]
        [Order(1)]
        public void GetEvents_AllOnSinglePage_ReturnsExpectedCount()
        {
            // Arrange
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась

            // Act
            var result = _eventService.GetEvents(null, null, null, 1, pageSize);

            // Assert
            Assert.Equal(result.TotalItems, result.Items.Count());
        }

        [Fact]
        [Order(1)]
        public void GetEventById_IfExists_ReturnsNotNull()
        {
            // Arrange
            var id = GetFirstExistingEventId();

            // Act
            var result = _eventService.GetById(id);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        [Order(1)]
        public void GetEventById_IfNotExists_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var id = -1;

            // Act & Assert
            Assert.Throws<EventNotFoundException>(() => _eventService.GetById(id));
        }

        [Fact]
        [Order(2)]
        public void UpdateEvent_IfExists_ReturnsUpdated()
        {
            // Arrange
            var expected = new Event() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };
            var id = GetFirstExistingEventId();

            // Act
            var result = _eventService.Update(id, expected);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.Title, result.Title);
            Assert.Equal(expected.Description, result.Description);
            Assert.Equal(expected.StartAt, result.StartAt);
            Assert.Equal(expected.EndAt, result.EndAt);
        }

        [Fact]
        [Order(2)]
        public void UpdateEvent_IfNotExists_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var envt = new Event() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };
            var id = -1;

            // Act & Assert
            Assert.Throws<EventNotFoundException>(() => _eventService.Update(id, envt));
        }

        [Fact]
        [Order(2)]
        public void UpdateEvent_IfDateInvalid_ThrowsValidationExeption()
        {
            // Arrange
            var evnt = new Event() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-05"), EndAt = Convert.ToDateTime("2026-07-02") };
            var id = -1;

            // Act & Assert
            Assert.Throws<ValidationException>(() => _eventService.Update(id, evnt));
        }

        [Fact]
        [Order(2)]
        public void DeleteEvent_IfExists_DoesNotContainEventWithDeletedId()
        {
            // Arrange
            var id = GetFirstExistingEventId();
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась

            // Act
            _eventService.Delete(id);
            var result = _eventService.GetEvents(null, null, null, 1, pageSize);

            // Assert
            Assert.DoesNotContain(id, result.Items.Select(e => e.Id));
        }

        [Fact]
        [Order(1)]
        public void GetEvents_FiltredByTitle_ReturnsExpectedTitles()
        {
            // Arrange
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась
            var titleFilter = "джаз";
            var expectedTitles = new[]
            {
                "Романтический джаз",
                "Джазовые портреты вокалистов",
                "Лэтин джаз",
                "Традиционный джаз и свинг",
                "Губная гармоника в джазе",
            };
            var notExpectedTitle = "Сказки Чижика";

            // Act
            var result = _eventService.GetEvents(titleFilter, null, null, 1, pageSize);

            // Assert
            Assert.All(expectedTitles, title => Assert.Contains(result.Items, e => e.Title == title));
            Assert.DoesNotContain(notExpectedTitle, result.Items.Select(e => e.Title));
        }

        [Fact]
        [Order(1)]
        public void GetEvents_FiltredByDates_ReturnsExpectedTitles()
        {
            // Arrange
            var pageSize = 1000000; // заведомо большой размер страницы, чтобы пагинация не применялась
            var startSince = Convert.ToDateTime("2026-10-12T00:00+03");
            var endTill = Convert.ToDateTime("2026-10-22T23:59+03");
            var expectedTitles = new[]
            {
                "Весь этот джаз",
                "Джем-сэшн",
                "Лэтин джаз",
            };
            var notExpectedTitle = "Петербургские мотивы";

            // Act
            var result = _eventService.GetEvents(null, startSince, endTill, 1, pageSize);

            // Assert
            Assert.All(expectedTitles, title => Assert.Contains(result.Items, e => e.Title == title));
            Assert.DoesNotContain(notExpectedTitle, result.Items.Select(e => e.Title));
        }

        [Fact]
        [Order(1)]
        public void GetEvents_FiltredByTitleAndDates_ReturnsExpectedTitles()
        {
            // Arrange
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась
            var titleFilter = "джаз";
            var startSince = Convert.ToDateTime("2026-10-12T00:00+03");
            var endTill = Convert.ToDateTime("2026-10-22T23:59+03");
            var expectedTitles = new[]
            {
                "Весь этот джаз",
                "Лэтин джаз",
            };
            var notExpectedTitle = "Джем-сэшн";

            // Act
            var result = _eventService.GetEvents(titleFilter, startSince, endTill, 1, pageSize);

            // Assert
            Assert.All(expectedTitles, title => Assert.Contains(result.Items, e => e.Title == title));
            Assert.DoesNotContain(notExpectedTitle, result.Items.Select(e => e.Title));
        }

        [Fact]
        [Order(1)]
        public void GetEvents_FiltredByDatesWhenIntervalIsEmpty_ReturnsNoEvents()
        {
            // Arrange
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась
            var startSince = Convert.ToDateTime("2026-10-22T00:00+03");
            var endTill = Convert.ToDateTime("2026-10-12T23:59+03");
            var expectedCount = 0;

            // Act
            var result = _eventService.GetEvents(null, startSince, endTill, 1, pageSize);

            // Assert
            Assert.Equal(expectedCount, result.TotalItems);
        }

        // Параметры фильтрации и пагинации (title, from, to, page, pageSize) + ожидаемое количество событий в выборке (expectedCount)
        public static TheoryData<string?, DateTime?, DateTime?, int, int, int> GetPaginationTestData()
        {
            var data = new TheoryData<string?, DateTime?, DateTime?, int, int, int>();

            data.Add("джаз", null, null, 1, 5, 5);
            data.Add("джаз", null, null, 2, 5, 1);
            data.Add("джаз", Convert.ToDateTime("2026-10-12T19:00:00+03"), null, 1, 100, 4);
            data.Add("джаз", null, Convert.ToDateTime("2026-10-12T19:00:00+03"), 1, 100, 2);
            data.Add(null, null, null, 0, 1, 1);

            return data;
        }

        [Theory(DisplayName = "Тестирование граничных условий для параметров фильтрации и пагинации.")]
        [MemberData(nameof(GetPaginationTestData))]
        [Order(1)] // Для тестов пагинации и фильтрациии задаем самый ранний порядок выполнения (до манипуляций со списком событий)
        public void GetEvents_BoundaryComplexCheck_ReturnsInline(string? title, DateTime? from, DateTime? to, int page, int pageSize, int expectedCount)
        {
            // Arrange

            // Act
            var result = _eventService.GetEvents(title, from, to, page, pageSize);

            // Assert
            Assert.Equal(expectedCount, result.Items.Count());
        }

        [Theory(DisplayName = "Тестирование пагинации: проверка количества событий на последней странице при разных pageSize.")]
        [InlineData(1, 12, 12)] // page, pageSize, expectedCount
        [InlineData(2, 11, 1)]
        [InlineData(2, 10, 2)]
        [InlineData(2, 9, 3)]
        [InlineData(2, 8, 4)]
        [InlineData(2, 7, 5)]
        [InlineData(2, 6, 6)]
        [InlineData(3, 5, 2)]
        [InlineData(3, 4, 4)]
        [InlineData(4, 3, 3)]
        [InlineData(6, 2, 2)]
        [InlineData(12, 1, 1)]
        [Order(1)] // Для тестов пагинации и фильтрациии задаем самый ранний порядок выполнения (до манипуляций со списком событий)
        public void GetEvents_WhenUsePagination_ReturnsCorrectLastPageEventsCount(int lastPage, int pageSize, int expectedCount)
        {
            // Arrange

            // Act
            var result = _eventService.GetEvents(null, null, null, lastPage, pageSize);

            // Assert
            Assert.Equal(expectedCount, result.OnPageItems);
        }
    }
}