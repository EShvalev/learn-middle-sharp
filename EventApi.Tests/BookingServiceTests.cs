using EventApi.Application.Bookings;
using EventApi.Application.Events;
using EventApi.Models.Bookings;
using EventApi.Models.Events;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Xunit.Extensions.Ordering;

namespace EventApi.Tests
{
    [Collection("GlobalCollection")]
    [Order(0)]
    public class BookingServiceTests //: IClassFixture<EventApiFixture>
    {
        private readonly IBookingStore _bookingStore;
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;
        private Guid _zeroGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
        private Guid _testGuid = Guid.Parse("00000002-0000-0000-0000-000000000000");
        private Guid _testGuidToDelete = Guid.Parse("00000005-0000-0000-0000-000000000000");

        public BookingServiceTests(EventApiFixture fixture)
        {
            _bookingStore = fixture.bookingStore;
            _bookingService = fixture.bookingService;
            _eventService = fixture.eventService;
        }

        [Fact]
        [Order(1)]
        public async Task CreateBooking_ExistingEvent_ReturnsId()
        {
            // Arrange
            var evnt = _eventService.GetById(_testGuid);

            // Act
            var booking = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Assert
            Assert.True(booking.Id != _zeroGuid);
        }

        [Fact]
        [Order(1)]
        public async Task CreateSeveralBookings_ExistingEvent_ReturnsUniqueIds()
        {
            // Arrange
            var evnt = _eventService.GetById(_testGuid);
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
        [Order(1)]
        public async Task GetBookingById_AfterCreating_ReturnsCorrectInfo()
        {
            // Arrange
            var evnt = _eventService.GetById(_testGuid);
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
        [Order(1)]
        public async Task GetBookingById_AfterConfirming_ReturnsCorrectStatus()
        {
            // Arrange
            var evnt = _eventService.GetById(_testGuid);
            var expected = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Act
            var booking = await _bookingService.GetBookingByIdAsync(expected.Id, CancellationToken.None);
            booking.Confirm();

            // Assert
            Assert.Equal(expected.Id, booking.Id);
            // После подтверждения статус должен быть Confirmed, поле ProcessedAt заполнено
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }

        [Fact]
        [Order(1)]
        public async Task GetBookingById_AfterRejecting_ReturnsCorrectStatus()
        {
            // Arrange
            var evnt = _eventService.GetById(_testGuid);
            var expected = await _bookingService.CreateBookingAsync(evnt.Id, CancellationToken.None);

            // Act
            var booking = await _bookingService.GetBookingByIdAsync(expected.Id, CancellationToken.None);
            booking.Reject();

            // Assert
            Assert.Equal(expected.Id, booking.Id);
            // После отмены статус должен быть Rejected, поле ProcessedAt заполнено
            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }

        [Fact]
        [Order(1)]
        public async Task GetBookingById_NotExistingId_ThrowsBookingNotFoundExeption()
        {
            // Arrange
            var id = _zeroGuid;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BookingNotFoundException>(() => _bookingService.GetBookingByIdAsync(id, CancellationToken.None));

            // Assert
            Assert.Equal(typeof(BookingNotFoundException), exception.GetType());
        }

        [Fact]
        [Order(1)]
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
        [Order(1)]
        public async Task CreateBooking_AfterDeletingEvent_ThrowsEventNotFoundExeption()
        {
            // Arrange
            var eventId = _testGuidToDelete;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => 
            {
                _eventService.Delete(eventId);
                await _bookingService.CreateBookingAsync(eventId, CancellationToken.None); 
            });

            // Assert
            Assert.Equal(typeof(EventNotFoundException), exception.GetType());
        }

        [Fact]
        [Order(1)]
        public async Task BookingStore_AddBooking_GetBooking()
        {
            // Arrange
            var eventId = _testGuid;
            var expected = Booking.CreatePending(_testGuid);

            // Act
            _bookingStore.Add(expected);
            _bookingStore.Get(expected.Id, out var booking);

            // Assert
            Assert.Equal(expected.Id, booking.Id);
        }

        [Fact]
        [Order(1)]
        public async Task Booking_CreateNew_GetStatus()
        {
            // Arrange
            var eventId = _testGuid;

            // Act
            var booking = Booking.CreatePending(_testGuid);

            // Assert
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.Null(booking.ProcessedAt);
        }

        [Fact]
        [Order(1)]
        public async Task Booking_Confirm_GetStatus()
        {
            // Arrange
            var eventId = _testGuid;

            // Act
            var booking = Booking.CreatePending(_testGuid);
            booking.Confirm();

            // Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }
    }
}
