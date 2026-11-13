using EventApi.Application.Events;
using EventApi.Models.Bookings;

namespace EventApi.Application.Bookings
{
    public class BookingService : IBookingService
    {
        private readonly IEventService _eventService;
        private readonly IBookingStore _bookingStore;
        private readonly ILogger<BookingService> _logger;

        // По замечанию ревью спринта 4 сделал _bookingLock статическим, чтобы при scoped регистрации сервиса вызовы из разных инстансов разделяли один и тот же локер
        private static readonly object _bookingLock = new();

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
            var evnt = await _eventService.GetEventByIdAsync(eventId, ct);
            Booking booking;

            lock (_bookingLock)
            {
                if (!evnt.TryReserveSeats())
                    throw new NoAvailableSeatsException("Недостаточно свободных мест для этого бронирования");

                // Вызов _eventService.UpdateEventAsync (сохранение события) здесь не нужен, т.к. поле AvailableSeats уже обновлено при вызове TryReserveSeats(), а другие поля события здесь не меняются.
                // Данный вызов будет нужен, когда хранилище событий в памяти будет заменено на репозиторий, работающий с БД.
                //_eventService.UpdateEventAsync(eventId, new CreateEventDto(evnt), ct);

                booking = Booking.Create(eventId);
                _bookingStore.Add(booking);
            }

            _logger.LogInformation("Создано бронирование {0} на событие {1}.", booking.Id, booking.EventId);
            return new BookingDto(booking);
        }

        public async Task<BookingDto> ConfirmBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = GetBookingById(id);

            booking.Confirm();
            _bookingStore.Update(booking.Id, booking);

            return new BookingDto(booking);
        }

        public async Task<BookingDto> RejectBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = GetBookingById(id);
            var evnt = await _eventService.GetEventByIdAsync(booking.EventId, ct);

            lock (_bookingLock)
            {
                booking.Reject();
                _bookingStore.Update(booking.Id, booking);

                evnt.ReleaseSeats();

                // Вызов _eventService.UpdateEventAsync (сохранение события) здесь не нужен, т.к. поле AvailableSeats уже обновлено при вызове ReleaseSeats(), а другие поля события здесь не меняются.
                // Данный вызов будет нужен, когда хранилище событий в памяти будет заменено на репозиторий, работающий с БД.
                //_eventService.UpdateEventAsync(booking.EventId, new CreateEventDto(evnt), ct);
            }

            return new BookingDto(booking);
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
