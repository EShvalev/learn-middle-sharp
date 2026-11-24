using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EventApi.Application.Bookings;
using EventApi.Application.Events;
using EventApi.Application.Exceptions;
using EventApi.DataAсcess;
using EventApi.Models.Bookings;

namespace EventApi.Tests
{
    //[Collection("GlobalCollection")]
    public class BookingServiceTests : IClassFixture<EventApiFixture>, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;
        private readonly IHostedService _bookingProcessingService;
        private readonly IServiceScope _scope;
        private readonly AppDbContext _dbContext;
        private readonly Guid _zeroGuid;
        private readonly Guid _testGuid;
        private readonly Guid _testGuidToDelete;

        public BookingServiceTests(EventApiFixture fixture)
        {
            _serviceProvider = fixture.serviceProvider;

            _bookingProcessingService = _serviceProvider//.GetRequiredService<BookingProcessingService>();
                .GetServices<IHostedService>()
                .OfType<BookingProcessingService>()
                .First();

            _scope = _serviceProvider.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
            _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();

            _zeroGuid = fixture.zeroGuid;
            _testGuid = fixture.testEventGuid;
            _testGuidToDelete = fixture.testEventGuidToDelete;
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        [Fact]
        public async Task CreateBooking_ExistingEvent_ReturnsId()
        {
            // Arrange
            var evnt = await _eventService.GetEventByIdAsync(_testGuid, CancellationToken.None);

            // Act
            var booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Assert
            Assert.True(booking.Id != _zeroGuid);
        }

        [Fact]
        public async Task CreateBooking_JustAfterCreating_ReturnsCorrectState()
        {
            // Arrange
            var evnt = await _eventService.GetEventByIdAsync(_testGuid, CancellationToken.None);

            // Act
            var booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Assert
            Assert.Equal(BookingStatus.Pending, booking.Status);
        }

        [Fact]
        public async Task CreateBooking_AfterProcessingByBPS_ReturnsCorrectState()
        {
            // Arrange
            var evnt = await _eventService.GetEventByIdAsync(_testGuid, CancellationToken.None);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)); // предельное время цикла, чтобы не зависал бескоонечно если что не так
            using var taskBPS = _bookingProcessingService.StartAsync(cts.Token);

            // Act
            BookingDto booking_before_processing = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
            BookingDto booking_after_processing = booking_before_processing;

            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(7000); // на каждой итерации ждем 7 сек = 5+2 (тик BPS таймера + задержка обработки)

                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                booking_after_processing = await bookingService.GetBookingByIdAsync(booking_before_processing.Id, CancellationToken.None);

                // Если статус брони изменился (не начальный), выходим из цикла через cts.Cancel (он же остановливает фоновый BPS)
                if (booking_after_processing.Status != BookingStatus.Pending)
                    cts.Cancel();
            }
            ;

            // Assert
            Assert.Equal(BookingStatus.Pending, booking_before_processing.Status);
            Assert.Equal(BookingStatus.Confirmed, booking_after_processing.Status);
        }

        [Fact]
        public async Task CreateSeveralBookings_ExistingEvent_ReturnsUniqueIds()
        {
            // Arrange
            var evnt = await _eventService.GetEventByIdAsync(_testGuid, CancellationToken.None);
            int count = 5;
            HashSet<Guid> ids = new HashSet<Guid>();

            // Act
            for (int i = 0; i < count; i++)
            {
                var booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);
                ids.Add(booking.Id);
            }

            // Assert
            Assert.DoesNotContain(_zeroGuid, ids);
            Assert.Equal(count, ids.Count());
        }

        [Fact]
        public async Task GetBookingById_AfterCreating_ReturnsCorrectInfo()
        {
            // Arrange
            var evnt = await _eventService.GetEventByIdAsync(_testGuid, CancellationToken.None);
            var expected = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Act
            var booking = await _bookingService.GetBookingByIdAsync(expected.Id, CancellationToken.None);

            // Assert
            Assert.Equal(expected.Id, booking.Id);

            // Сразу после создания статус должен быть Pending, поле ProcessedAt не заполнено
            Assert.Null(booking.ProcessedAt);
            Assert.Equal(BookingStatus.Pending, booking.Status);
        }

        [Fact]
        public async Task GetBookingById_NotExistingId_ThrowsBookingNotFoundExeption()
        {
            // Arrange
            var id = _zeroGuid;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.GetBookingByIdAsync(id, CancellationToken.None));

            // Assert
            Assert.Equal(typeof(NotFoundException), exception.GetType());
        }

        [Fact]
        public async Task CreateBooking_NotExistingEvent_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var eventId = _zeroGuid;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(eventId, CancellationToken.None));

            // Assert
            Assert.Equal(typeof(NotFoundException), exception.GetType());
        }

        [Fact]
        public async Task CreateBooking_AfterDeletingEvent_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var eventId = _testGuidToDelete;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _eventService.DeleteAsync(eventId, CancellationToken.None);
                await _bookingService.CreateBookingAsync(eventId, CancellationToken.None);
            });

            // Assert
            Assert.Equal(typeof(NotFoundException), exception.GetType());
        }

        [Fact]
        public async Task BookingStore_AddBooking_GetBooking()
        {
            // Arrange
            var eventId = _testGuid;
            var expected = Booking.Create(_testGuid);

            // Act
            await _dbContext.Bookings.AddAsync(expected, CancellationToken.None); //_bookingStore.CreateAsync(expected, CancellationToken.None);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            var booking = await _dbContext.Bookings.FindAsync(expected.Id, CancellationToken.None); //_bookingStore.GetByIdAsync(expected.Id, CancellationToken.None);

            // Assert
            Assert.Equal(expected.Id, booking.Id);
        }

        [Fact]
        public async Task Booking_CreateNew_GetStatus()
        {
            // Arrange
            var eventId = _testGuid;

            // Act
            var booking = Booking.Create(_testGuid);

            // Assert
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.Null(booking.ProcessedAt);
        }

        [Fact]
        public async Task Booking_Confirm_GetStatus()
        {
            // Arrange
            var eventId = _testGuid;

            // Act
            var booking = Booking.Create(_testGuid);
            booking.Confirm();

            // Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }
    }
}
