using EventApi.Models.Bookings;
using EventApi.Models.Events;

namespace EventApi.Application.Bookings
{
    public class BookingService : IBookingService
    {
        private readonly InMemoryBookingStore _bookingStore;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            InMemoryBookingStore bookingStore,
            ILogger<BookingService> logger
            )
        { 
            _bookingStore = bookingStore;
            _logger = logger;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
        {
            var booking = Booking.CreatePending(eventId);

            _bookingStore.Add(booking);
            
            // Имитирум задержку
            await Task.Delay(100, ct);

            _logger.LogInformation("Создано бронирование {0} на событие {1}.", booking.Id, booking.EventId);
            return booking;
        }

        public async Task<Booking> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (!_bookingStore.Get(id, out var booking))
                throw new BookingNotFoundException(string.Format("Бронирование {0} не найдено.", id));

            // Имитирум задержку
            await Task.Delay(100, ct);

            return booking;
        }
    }
}
