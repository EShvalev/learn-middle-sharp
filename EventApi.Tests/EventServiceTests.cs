using EventApi.Application.Events;
using EventApi.Models.Events;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Tests
{
    public class EventServiceTests : IClassFixture<EventApiFixture>
    {
        private readonly IEventService _eventService;
        private Guid _zeroGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
        private Guid _testGuid = Guid.Parse("00000005-0000-0000-0000-000000000000");
        private Guid _testGuidToDelete = Guid.Parse("00000001-0000-0000-0000-000000000000");
        private readonly int _VeryLongPageSize = 1000000;

        public EventServiceTests(EventApiFixture fixture)
        {
            _eventService = fixture.eventService;
        }

        [Fact]
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
        public void GetEventById_IfExists_ReturnsNotNull()
        {
            // Arrange
            var id = _testGuid;

            // Act
            var result = _eventService.GetById(id);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetEventById_IfNotExists_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var id = _zeroGuid;

            // Act & Assert
            Assert.Throws<EventNotFoundException>(() => _eventService.GetById(id));
        }

        [Fact]
        public void CreateEvent_OneEvent_ReturnsId()
        {
            // Arrange
            var evnt = new Event() { Id = Guid.NewGuid(), Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };

            // Act
            var id = _eventService.Create(evnt);

            // Assert
            Assert.True(id != _zeroGuid);
        }

        [Fact]
        public void CreateEvent_IfDateInvalid_ThrowsValidationExeption()
        {
            // Arrange
            var evnt = new Event() { Id = Guid.NewGuid(), Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-05"), EndAt = Convert.ToDateTime("2026-07-02") };

            // Act & Assert
            Assert.Throws<ValidationException>(() => _eventService.Create(evnt));
        }

        [Fact]
        public void UpdateEvent_IfExists_ReturnsUpdated()
        {
            // Arrange
            var expected = new Event() { Id = Guid.NewGuid(), Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };
            var id = _testGuid;

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
        public void UpdateEvent_IfNotExists_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var envt = new Event() { Id = Guid.NewGuid(), Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };
            var id = _zeroGuid;

            // Act & Assert
            Assert.Throws<EventNotFoundException>(() => _eventService.Update(_zeroGuid, envt));
        }

        [Fact]
        public void UpdateEvent_IfDateInvalid_ThrowsValidationExeption()
        {
            // Arrange
            var evnt = new Event() { Id = Guid.NewGuid(), Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-05"), EndAt = Convert.ToDateTime("2026-07-02") };
            var id = _zeroGuid;

            // Act & Assert
            Assert.Throws<ValidationException>(() => _eventService.Update(id, evnt));
        }

        [Fact]
        public void DeleteEvent_IfExists_DoesNotContainEventWithDeletedId()
        {
            // Arrange
            var id = _testGuidToDelete;
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась

            // Act
            _eventService.Delete(id);
            var result = _eventService.GetEvents(null, null, null, 1, pageSize);

            // Assert
            Assert.DoesNotContain(id, result.Items.Select(e => e.Id));
        }
    }
}