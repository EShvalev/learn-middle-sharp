using EventApi.Application.Events;
using EventApi.Models.Events;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Tests
{
    public class EventServiceFiltPagTests : IClassFixture<EventApiFixture>
    {
        private readonly IEventService _eventService;
        private Guid _zeroGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
        private Guid _testGuid = Guid.Parse("00000005-0000-0000-0000-000000000000");
        private readonly int _VeryLongPageSize = 1000000;

        public EventServiceFiltPagTests(EventApiFixture fixture)
        {
            _eventService = fixture.eventService;
        }

        [Fact]
        public async Task GetEvents_FiltredByTitle_ReturnsExpectedTitles()
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
            var result = await _eventService.GetEventsAsync(titleFilter, null, null, 1, pageSize, CancellationToken.None);

            // Assert
            Assert.All(expectedTitles, title => Assert.Contains(result.Items, e => e.Title == title));
            Assert.DoesNotContain(notExpectedTitle, result.Items.Select(e => e.Title));
        }

        [Fact]
        public async Task GetEvents_FiltredByDates_ReturnsExpectedTitles()
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
            var result = await _eventService.GetEventsAsync(null, startSince, endTill, 1, pageSize, CancellationToken.None);

            // Assert
            Assert.All(expectedTitles, title => Assert.Contains(result.Items, e => e.Title == title));
            Assert.DoesNotContain(notExpectedTitle, result.Items.Select(e => e.Title));
        }

        [Fact]
        public async Task GetEvents_FiltredByTitleAndDates_ReturnsExpectedTitles()
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
            var result = await _eventService.GetEventsAsync(titleFilter, startSince, endTill, 1, pageSize, CancellationToken.None);

            // Assert
            Assert.All(expectedTitles, title => Assert.Contains(result.Items, e => e.Title == title));
            Assert.DoesNotContain(notExpectedTitle, result.Items.Select(e => e.Title));
        }

        [Fact]
        public async Task GetEvents_FiltredByDatesWhenIntervalIsEmpty_ReturnsNoEvents()
        {
            // Arrange
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась
            var startSince = Convert.ToDateTime("2026-10-22T00:00+03");
            var endTill = Convert.ToDateTime("2026-10-12T23:59+03");
            var expectedCount = 0;

            // Act
            var result = await _eventService.GetEventsAsync(null, startSince, endTill, 1, pageSize, CancellationToken.None);

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
            data.Add(null, null, null, 1, 1, 1);

            return data;
        }

        [Theory(DisplayName = "Тестирование граничных условий для параметров фильтрации и пагинации.")]
        [MemberData(nameof(GetPaginationTestData))]
        public async Task GetEvents_BoundaryComplexCheck_ReturnsInline(string? title, DateTime? from, DateTime? to, int page, int pageSize, int expectedCount)
        {
            // Arrange

            // Act
            var result = await _eventService.GetEventsAsync(title, from, to, page, pageSize, CancellationToken.None);

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
        public async Task GetEvents_WhenUsePagination_ReturnsCorrectLastPageEventsCount(int lastPage, int pageSize, int expectedCount)
        {
            // Arrange

            // Act
            var result = await _eventService.GetEventsAsync(null, null, null, lastPage, pageSize, CancellationToken.None);

            // Assert
            Assert.Equal(expectedCount, result.CurrentPageSize);
        }
    }
}