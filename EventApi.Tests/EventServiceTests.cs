using EventApi.Application.Events;
using EventApi.Models.Events;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Tests
{
    public class EventServiceTests : IClassFixture<EventApiFixture>
    {
        private readonly IEventService _eventService;
        private readonly Guid _zeroGuid;
        private readonly Guid _testGuid;
        private readonly Guid _testGuidToDelete;
        private readonly int _VeryLongPageSize = 1000000;

        public EventServiceTests(EventApiFixture fixture)
        {
            _eventService = fixture.eventService;
            _zeroGuid = fixture.zeroGuid;
            _testGuid = fixture.testEventGuid;
            _testGuidToDelete = fixture.testEventGuidToDelete;
        }

        [Fact]
        public async Task GetEvents_AllOnSinglePage_ReturnsExpectedCount()
        {
            // Arrange
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась

            // Act
            var result = await _eventService.GetEventsAsync(null, null, null, 1, pageSize, CancellationToken.None);

            // Assert
            Assert.Equal(result.TotalItems, result.Items.Count());
        }

        [Fact]
        public async Task GetEventById_IfExists_ReturnsNotNull()
        {
            // Arrange
            var id = _testGuid;

            // Act
            var result = await _eventService.GetEventByIdAsync(id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetEventById_IfNotExists_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var id = _zeroGuid;

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(() => _eventService.GetEventByIdAsync(id, CancellationToken.None));
        }

        [Fact]
        public async Task CreateEvent_OneEvent_ReturnsId()
        {
            // Arrange
            var evnt = new CreateEventDto() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };

            // Act
            var dto = await _eventService.CreateEventAsync(evnt, CancellationToken.None);

            // Assert
            Assert.True(dto.Id != _zeroGuid);
        }

        [Fact]
        public async Task CreateEvent_IfDateInvalid_ThrowsValidationExeption()
        {
            // Arrange
            var evnt = new CreateEventDto() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-05"), EndAt = Convert.ToDateTime("2026-07-02") };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(evnt, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateEvent_IfExists_ReturnsUpdated()
        {
            // Arrange
            var expected = new CreateEventDto() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };
            var id = _testGuid;

            // Act
            var result = await _eventService.UpdateEventAsync(id, expected, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.Title, result.Title);
            Assert.Equal(expected.Description, result.Description);
            Assert.Equal(expected.StartAt, result.StartAt);
            Assert.Equal(expected.EndAt, result.EndAt);
        }

        [Fact]
        public async Task UpdateEvent_IfNotExists_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var envt = new CreateEventDto() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-02"), EndAt = Convert.ToDateTime("2026-07-05") };
            var id = _zeroGuid;

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(() => _eventService.UpdateEventAsync(_zeroGuid, envt, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateEvent_IfDateInvalid_ThrowsValidationExeption()
        {
            // Arrange
            var evnt = new CreateEventDto() { Title = "Свинг белой ночи", Description = "Ежегодный джазовый фестиваль в СПб", StartAt = Convert.ToDateTime("2026-07-05"), EndAt = Convert.ToDateTime("2026-07-02") };
            var id = _testGuid;

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _eventService.UpdateEventAsync(id, evnt, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteEvent_IfExists_DoesNotContainEventWithDeletedId()
        {
            // Arrange
            var id = _testGuidToDelete;
            var pageSize = _VeryLongPageSize; // заведомо большой размер страницы, чтобы пагинация не применялась

            // Act
            await _eventService.DeleteAsync(id, CancellationToken.None);
            var result = await _eventService.GetEventsAsync(null, null, null, 1, pageSize, CancellationToken.None);

            // Assert
            Assert.DoesNotContain(id, result.Items.Select(e => e.Id));
        }
    }
}