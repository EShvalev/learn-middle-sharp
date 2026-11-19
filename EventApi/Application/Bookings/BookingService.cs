using EventApi.Application.Events;
using EventApi.Models.Bookings;
using EventApi.Models.Events;

namespace EventApi.Application.Bookings
{
    public class BookingService : IBookingService
    {
        private readonly IKeyedLockManager _lockManager;
        private readonly IEventStore _eventStore;
        private readonly IBookingStore _bookingStore;
        private readonly ILogger<BookingService> _logger;

        // По замечанию ревью спринта 4 сделал _bookingLock статическим, чтобы при scoped регистрации сервиса вызовы из разных инстансов разделяли один и тот же локер
        // 19.11.2026: по замечанию из ревью спринта 4 заменил статический lock на синглтон объект IKeyedLockManager
        //private static readonly object _bookingLock = new();

        public BookingService(
            IKeyedLockManager lockManager,
            IEventStore EventStore,
            IBookingStore bookingStore,
            ILogger<BookingService> logger
            )
        {
            _lockManager = lockManager;
            _eventStore = EventStore;
            _bookingStore = bookingStore;
            _logger = logger;
        }

        public async Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
        {
            Booking booking;

            //lock (_bookingLock)
            using (await _lockManager.AcquireEventLockAsync(eventId, ct))
            {
                var evnt = await _eventStore.GetByIdAsync(eventId, ct);
                if (evnt == null)
                    throw new EventNotFoundException($"Событие {eventId} не найдено. Бронирование по нему не создано.");

                if (!evnt.TryReserveSeats())
                    throw new NoAvailableSeatsException("Недостаточно свободных мест для этого бронирования");

                // Вызов _EventStore.UpdateEventAsync (сохранение события) здесь не нужен, т.к. поле AvailableSeats уже обновлено при вызове TryReserveSeats(), а другие поля события здесь не меняются.
                // Данный вызов будет нужен, когда хранилище событий в памяти будет заменено на репозиторий, работающий с БД.
                //_eventService.UpdateEventAsync(eventId, new CreateEventDto(evnt), ct);

                // После замены _eventService на _eventStore вызов Update нужен, т.к. все изменения теперь производятся не напрямую в объектах хранилища, а через клоны.
                await _eventStore.UpdateAsync(eventId, evnt, ct);

                booking = Booking.Create(eventId);
                await _bookingStore.CreateAsync(booking, ct);
            }

            _logger.LogInformation("Создано бронирование {0} на событие {1}.", booking.Id, booking.EventId);
            return new BookingDto(booking);
        }

        public async Task<BookingDto> ConfirmBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = await _bookingStore.GetByIdAsync(id, ct);
            if (booking == null)
                throw new EventNotFoundException($"Бронирование {id} не найдено.");

            booking.Confirm();
            await _bookingStore.UpdateAsync(booking.Id, booking, ct);

            return new BookingDto(booking);
        }

        public async Task<BookingDto> RejectBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = await _bookingStore.GetByIdAsync(id, ct);
            if (booking == null)
                throw new EventNotFoundException($"Бронирование {id} не найдено.");

            //lock (_bookingLock)
            using (await _lockManager.AcquireEventLockAsync(booking.EventId, ct))
            {
                booking.Reject();
                await _bookingStore.UpdateAsync(booking.Id, booking, ct);

                var evnt = _eventStore.GetByIdAsync(booking.EventId, ct).Result;
                if (evnt == null)
                    throw new EventNotFoundException($"Событие {booking.EventId} не найдено. Отмены бронирования по нему не произошло.");

                evnt.ReleaseSeats();

                // Вызов _EventStore.UpdateEventAsync (сохранение события) здесь не нужен, т.к. поле AvailableSeats уже обновлено при вызове ReleaseSeats(), а другие поля события здесь не меняются.
                // Данный вызов будет нужен, когда хранилище событий в памяти будет заменено на репозиторий, работающий с БД.
                //_EventStore.UpdateEventAsync(booking.EventId, new CreateEventDto(evnt), ct);

                // После замены _eventService на _eventStore вызов Update нужен, т.к. все изменения теперь производятся не напрямую в объектах хранилища, а через клоны.
                await _eventStore.UpdateAsync(booking.EventId, evnt, ct);
            }

            return new BookingDto(booking);
        }

        public async Task<BookingDto> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            var booking = await _bookingStore.GetByIdAsync(id, ct);
            if (booking == null)
                throw new BookingNotFoundException(string.Format("Бронирование {0} не найдено.", id));

            return new BookingDto(booking);
        }

        public async Task<List<BookingDto>> GetBookingsAsync(CancellationToken ct = default)
        {
            // Список всех бронирований
            var bookings = await _bookingStore.GetAllAsync(ct);
            var dtos = bookings.Select(b => new BookingDto(b)).ToList();

            return dtos;
        }
    }
}
