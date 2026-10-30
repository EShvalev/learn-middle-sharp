using EventApi.Application.Events;
using EventApi.Models.Bookings;

namespace EventApi.Application.Bookings
{
    public class BookingService : IBookingService
    {
        private readonly IEventService _eventService;
        private readonly IBookingStore _bookingStore;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IEventService eventService,
            IBookingStore bookingStore,
            ILogger<BookingService> logger
            )
        {
            _eventService = eventService;
            _bookingStore = bookingStore;
            _logger = logger;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
        {
            // Этот вызов нужен чтобы проверить существование события с данным id (если нет, выбрасывается исключение).
            var evnt = _eventService.GetById(eventId);

            var booking = Booking.CreatePending(eventId);

            _bookingStore.Add(booking);

            _logger.LogInformation("Создано бронирование {0} на событие {1}.", booking.Id, booking.EventId);
            return booking;
        }

        public async Task<Booking> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (!_bookingStore.Get(id, out var booking))
                throw new BookingNotFoundException(string.Format("Бронирование {0} не найдено.", id));

            return booking;
        }

        public async Task<List<Booking>> GetBookingsAsync(CancellationToken ct = default)
        {
            // Список всех бронирований
            var bookings = _bookingStore.GetAll().ToList<Booking>();

            return bookings;
        }
    }
}
