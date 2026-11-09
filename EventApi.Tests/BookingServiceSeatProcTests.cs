using EventApi.Application.Bookings;
using EventApi.Application.Events;
using EventApi.Models.Bookings;
using EventApi.Models.Events;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Reflection;

namespace EventApi.Tests
{
    //[Collection("GlobalCollection")]
    public class BookingServiceSeatProcTests : IClassFixture<EventApiFixture>
    {
        private readonly IBookingStore _bookingStore;
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;
        private readonly IHostedService _bookingProcessingService;
        private readonly Guid _zeroGuid;
        private readonly Guid _testGuid;
        private readonly Guid _testGuidToDelete;

        public BookingServiceSeatProcTests(EventApiFixture fixture)
        {
            _bookingStore = fixture.bookingStore;
            _bookingService = fixture.bookingService;
            _eventService = fixture.eventService;

            _zeroGuid = fixture.zeroGuid;
            _testGuid = fixture.testEventGuid;
            _testGuidToDelete = fixture.testEventGuidToDelete;

            _bookingProcessingService = fixture.bookingProcessingService;
        }

        // Вспомогательный метод для тестов: создает тестовое событие.
        private Event CreateTestEvent(int totalSeats)
        {
            var createDto =  new CreateEventDto() 
            { 
                Title = "Джазовые портреты вокалистов", 
                Description = "Звезды мирового джаза", 
                StartAt = Convert.ToDateTime("2026-10-09T19:00:00+03"), 
                EndAt = Convert.ToDateTime("2026-10-09T21:00:00+03"), 
                TotalSeats = totalSeats
            };
            
            var dto = _eventService.CreateEventAsync(createDto, CancellationToken.None).Result;
            var evnt = _eventService.GetEventByIdAsync(dto.Id, CancellationToken.None);

            return evnt.Result;
        }

        // Вспомогательный метод для тестов: устанавливает заданное TotalSeats.
        private void ResetEventSeats(Event evnt, int totalSeats)
        {
            evnt.Update(
                title: evnt.Title,
                description: evnt.Description,
                startAt: evnt.StartAt,
                endAt: evnt.EndAt,
                totalSeats: totalSeats,
                withValidation: false
                );
        }

        [Fact]
        public async Task CreateBooking_JustAfterCreating_ReturnsCorrectStateAndSeats()
        {
            // Arrange
            var evnt = CreateTestEvent(totalSeats: 1);
            int expectedSeats = evnt.AvailableSeats - 1;

            Console.WriteLine($"evnt.TotalSeats = {evnt.TotalSeats}, evnt.AvailableSeats = {evnt.AvailableSeats}");

            // Act
            var booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Assert
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.Equal(expectedSeats, evnt.AvailableSeats);
        }

        [Fact]
        public async Task CreateSeveralBookings_ExistingEvent_ReturnsUniqueIdsAndZeroAvailableSeats()
        {
            // Arrange
            var evnt = CreateTestEvent(totalSeats: 5);
            int count = evnt.AvailableSeats;
            HashSet<Guid> ids = new HashSet<Guid>();
            int expectedSeats = 0;

            // Act
            for (int i = 0; i < count; i++)
            {
                var booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
                ids.Add(booking.Id);
            }

            // Assert
            Assert.DoesNotContain(_zeroGuid, ids);
            Assert.Equal(count, ids.Count());
            Assert.Equal(expectedSeats, evnt.AvailableSeats);
        }

        [Fact]
        public async Task CreateSeveralBookings_ExistingEvent_ThrowsNoAvailableSeatsExceptionWhenOverbook()
        {
            // Arrange
            var evnt = CreateTestEvent(totalSeats: 2);
            int count = evnt.AvailableSeats + 1; // Overbook!!!

            // Act
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                for (int i = 0; i < count; i++)
                {
                    await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
                }
            });

            // Assert
            Assert.Equal(typeof(NoAvailableSeatsException), exception.GetType());
        }

        [Fact]
        public async Task CreateBooking_NotExistingEvent_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var eventId = _zeroGuid;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<EventNotFoundException>(() => _bookingService.CreateBookingAsync(eventId, CancellationToken.None));

            // Assert
            Assert.Equal(typeof(EventNotFoundException), exception.GetType());
        }

        [Fact]
        public async Task CreateBooking_ExistingEvent_ThrowsNoAvailableSeatsExceptionWhenNoAvailSeats()
        {
            // Arrange
            var evnt = CreateTestEvent(totalSeats: 1);
            ResetEventSeats(evnt, 0);

            // Act
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () => await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None));

            // Assert
            Assert.Equal(typeof(NoAvailableSeatsException), exception.GetType());
        }

        [Fact]
        public async Task CreateBooking_AfterConfirming_ReturnsCorrectStatusAndSeats()
        {
            // Arrange
            var evnt = CreateTestEvent(totalSeats: 1);
            var expected = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
            int expectedSeats = evnt.AvailableSeats; // после отмены брони количество свободных мест не должно измениться

            // Act
            var booking = await _bookingService.ConfirmBookingAsync(expected.Id, CancellationToken.None);

            // Assert
            Assert.Equal(expected.Id, booking.Id);
            // После подтверждения статус должен быть Confirmed, поле ProcessedAt заполнено
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
            // AvailableSeats после подтверждения брони не меняется
            Assert.Equal(expectedSeats, evnt.AvailableSeats);
        }

        [Fact]
        public async Task CreateBooking_AfterRejecting_ReturnsCorrectStatusAndSeats()
        {
            // Arrange
            var evnt = CreateTestEvent(totalSeats: 1);
            var expected = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
            int expectedSeats = evnt.AvailableSeats + 1; // после отмены брони одно свободное место должно вернуться

            // Act
            var booking = await _bookingService.RejectBookingAsync(expected.Id, CancellationToken.None);

            // Assert
            Assert.Equal(expected.Id, booking.Id);
            // После отмены статус должен быть Rejected, поле ProcessedAt заполнено
            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
            // AvailableSeats после отмены брони
            Assert.Equal(expectedSeats, evnt.AvailableSeats);
        }

        [Fact]
        public async Task CreateBooking_AfterRejecting_CanCreateAnotherBooking()
        {
            // Arrange
            var evnt = CreateTestEvent(totalSeats: 1);
            var first_booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Act
            await _bookingService.RejectBookingAsync(first_booking.Id, CancellationToken.None);
            var another_booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Assert
            // Вторая бронь создана в начальном статусе
            Assert.Equal(BookingStatus.Pending, another_booking.Status);
            Assert.Null(another_booking.ProcessedAt);
        }

        [Fact]
        public async Task CreateConcurrentBookings_ExistingEvent_ReturnsCorrectSuccessCountAndExceptionCount()
        {
            // Arrange
            var evnt = CreateTestEvent(totalSeats: 5);

            int count = 20; // 5 должно создаться успешно + 15 NoAvailableSeatsException
            List<Task> allTasks = new List<Task>();

            for (int i = 0; i < count; i++)
            {
                allTasks.Add(Task.Run(() => _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None)));
            }

            // Act
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await Task.WhenAll(allTasks);
            });

            // Assert
            Assert.Equal(5, allTasks.Where(t => t.IsCompletedSuccessfully).Count());
            Assert.Equal(15, allTasks.Where(t => t.IsFaulted && typeof(NoAvailableSeatsException) == t.Exception?.InnerException?.GetType()).Count());
        }

        [Fact]
        public async Task CreateConcurrentBookings_ExistingEvent_ReturnsCorrectUniqueIdsCount()
        {
            // Arrange
            int count = 10; // должны создаться успешно с уникальными Id
            var evnt = CreateTestEvent(totalSeats: count);

            List<Task> allTasks = new List<Task>();
            ConcurrentBag<Guid> ids = new ConcurrentBag<Guid>();

            // Act
            for (int i = 0; i < count; i++)
            {
                allTasks.Add(Task.Run(() =>
                {
                    var booking = _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
                    ids.Add(booking.Result.Id);
                }
                ));
            }

            await Task.WhenAll(allTasks);

            // Assert
            Assert.Equal(count, ids.Count());
        }
    }
}
