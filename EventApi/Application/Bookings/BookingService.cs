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

        public async Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
        {
            // Этот вызов нужен чтобы проверить существование события с данным id (если нет, выбрасывается исключение).
            var evnt = _eventService.GetEventByIdAsync(eventId, ct);

            var booking = Booking.CreatePending(eventId);

            _bookingStore.Add(booking);

            _logger.LogInformation("Создано бронирование {0} на событие {1}.", booking.Id, booking.EventId);
            return new BookingDto(booking);
        }

        public Task<BookingDto> ConfirmBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = GetBookingById(id);
            
            booking.Confirm();

            return Task.FromResult(new BookingDto(booking));
        }

        public Task<BookingDto> RejectBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = GetBookingById(id);

            booking.Reject();

            return Task.FromResult(new BookingDto(booking));
        }

        private Booking GetBookingById(Guid id)
        {
            if (!_bookingStore.Get(id, out var booking))
                throw new BookingNotFoundException(string.Format("Бронирование {0} не найдено.", id));

            return booking;
        }

        public async Task<BookingDto> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            var booking = GetBookingById(id);

            return new BookingDto(booking);
        }

        public async Task<List<BookingDto>> GetBookingsAsync(CancellationToken ct = default)
        {
            // Список всех бронирований
            var bookings = _bookingStore.GetAll().Select(b => new BookingDto(b)).ToList();

            return bookings;
        }
    }
}
