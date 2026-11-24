using EventApi.Application.Bookings;
using EventApi.Application.Events;
using EventApi.DataAсcess;
using EventApi.Models.Bookings;
using EventApi.Models.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace EventApi.Tests
{
    //[Collection("GlobalCollection")]
    public class BookingServiceSeatProcTests : IClassFixture<EventApiFixture>, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBookingService _bookingService;
        private readonly IServiceScope _scope;
        private readonly AppDbContext _dbContext;
        private readonly Guid _zeroGuid;

        public BookingServiceSeatProcTests(EventApiFixture fixture)
        {
            _serviceProvider = fixture.serviceProvider;
            _scope = _serviceProvider.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();

            _zeroGuid = fixture.zeroGuid;
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        // Вспомогательный метод для тестов: создает тестовое событие.
        private async Task<Event> CreateTestEvent(int totalSeats)
        {
            var dto = new CreateEventDto()
            {
                Title = "Джазовые портреты вокалистов",
                Description = "Звезды мирового джаза",
                StartAt = Convert.ToDateTime("2026-10-09T19:00:00+03"),
                EndAt = Convert.ToDateTime("2026-10-09T21:00:00+03"),
                TotalSeats = totalSeats
            };

            var evnt = Event.Create(
                title: dto.Title,
                description: dto.Description,
                startAt: dto.StartAt,
                endAt: dto.EndAt,
                totalSeats: dto.TotalSeats,
                withValidation: true
            );

            await _dbContext.Events.AddAsync(evnt, CancellationToken.None);
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            return evnt;
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
            var evnt = await CreateTestEvent(totalSeats: 1);
            int expectedSeats = evnt.AvailableSeats - 1;

            // Act
            var booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
            // После манипуляций с бронированием событие нужно запрашивать из хранилища, т.к. именно в нем содержится актуальная версия состояния
            //await _dbContext.Entry(evnt).ReloadAsync(CancellationToken.None); //_eventStore.GetByIdAsync(evnt.Id, CancellationToken.None);
            evnt = await _dbContext.Events.FindAsync(evnt.Id, CancellationToken.None);

            // Assert
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.Equal(expectedSeats, evnt.AvailableSeats);
        }

        [Fact]
        public async Task CreateSeveralBookings_ExistingEvent_ReturnsUniqueIdsAndZeroAvailableSeats()
        {
            // Arrange
            var evnt = await CreateTestEvent(totalSeats: 5);
            int count = evnt.AvailableSeats;
            HashSet<Guid> ids = new HashSet<Guid>();
            int expectedSeats = 0;

            // Act
            for (int i = 0; i < count; i++)
            {
                var booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
                ids.Add(booking.Id);
            }
            // После манипуляций с бронированием событие нужно запрашивать из хранилища, т.к. именно в нем содержится актуальная версия состояния
            //await _dbContext.Entry(evnt).ReloadAsync(CancellationToken.None); //_eventStore.GetByIdAsync(evnt.Id, CancellationToken.None);
            evnt = await _dbContext.Events.FindAsync(evnt.Id, CancellationToken.None);

            // Assert
            Assert.DoesNotContain(_zeroGuid, ids);
            Assert.Equal(count, ids.Count());
            Assert.Equal(expectedSeats, evnt.AvailableSeats);
        }

        [Fact]
        public async Task CreateSeveralBookings_ExistingEvent_ThrowsNoAvailableSeatsExceptionWhenOverbook()
        {
            // Arrange
            var evnt = await CreateTestEvent(totalSeats: 2);
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
        public async Task CreateBooking_ExistingEvent_ThrowsNoAvailableSeatsExceptionWhenNoAvailSeats()
        {
            // Arrange
            var evnt = await CreateTestEvent(totalSeats: 1);

            // Act
            await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None); // резевируем одно место, т.к. всего было одно, следующий вызов должен выбросить исключение

            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () => await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None));

            // Assert
            Assert.Equal(typeof(NoAvailableSeatsException), exception.GetType());
        }

        [Fact]
        public async Task CreateBooking_AfterConfirming_ReturnsCorrectStatusAndSeats()
        {
            // Arrange
            var evnt = await CreateTestEvent(totalSeats: 1);
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
            var evnt = await CreateTestEvent(totalSeats: 1);
            var expected = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
            // После манипуляций с бронированием событие нужно запрашивать из хранилища, т.к. именно в нем содержится актуальная версия состояния
            //await _dbContext.Entry(evnt).ReloadAsync(CancellationToken.None); //_eventStore.GetByIdAsync(evnt.Id, CancellationToken.None);
            evnt = await _dbContext.Events.FindAsync(evnt.Id, CancellationToken.None);
            int expectedSeats = evnt.AvailableSeats + 1; // после отмены брони одно свободное место должно вернуться

            // Act
            var booking = await _bookingService.RejectBookingAsync(expected.Id, CancellationToken.None);
            // После манипуляций с бронированием событие нужно запрашивать из хранилища, т.к. именно в нем содержится актуальная версия состояния
            //await _dbContext.Entry(evnt).ReloadAsync(CancellationToken.None); //_eventStore.GetByIdAsync(evnt.Id, CancellationToken.None);
            evnt = await _dbContext.Events.FindAsync(evnt.Id, CancellationToken.None);

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
            var evnt = await CreateTestEvent(totalSeats: 1);
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
            var evnt = await CreateTestEvent(totalSeats: 5);
            int expectedSeats = 0;

            int count = 20; // 5 должно создаться успешно + 15 NoAvailableSeatsException
            var allTasks = Enumerable.Range(0, count)
                .Select(_ => Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    await bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
                }))
                .ToList();

            // Act
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await Task.WhenAll(allTasks);
            });
            // После манипуляций с бронированием событие нужно перезапрашивать из хранилища, т.к. в Task.Run создавались локальные БД контексты.
            await _dbContext.Entry(evnt).ReloadAsync(CancellationToken.None);

            // Assert
            Assert.Equal(5, allTasks.Where(t => t.IsCompletedSuccessfully).Count());
            Assert.Equal(15, allTasks.Where(t => t.IsFaulted && typeof(NoAvailableSeatsException) == t.Exception?.InnerException?.GetType()).Count());
            Assert.Equal(expectedSeats, evnt.AvailableSeats);
        }

        [Fact]
        public async Task CreateConcurrentBookings_ExistingEvent_ReturnsCorrectUniqueIdsCount()
        {
            // Arrange
            int count = 10; // должны создаться успешно с уникальными Id
            var evnt = await CreateTestEvent(totalSeats: count);
            int expectedSeats = 0;

            // Act
            var ids = new ConcurrentBag<Guid>();
            var allTasks = Enumerable.Range(0, count)
                .Select(_ => Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    var booking = bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
                    ids.Add(booking.Result.Id);
                }
                ));

            await Task.WhenAll(allTasks);
            // После манипуляций с бронированием событие нужно перезапрашивать из хранилища, т.к. в Task.Run создавались локальные БД контексты.
            await _dbContext.Entry(evnt).ReloadAsync(CancellationToken.None);

            // Assert
            Assert.Equal(count, ids.Distinct().Count());
            Assert.Equal(expectedSeats, evnt.AvailableSeats);
        }
    }
}
