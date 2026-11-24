using EventApi.DataAсcess;
using EventApi.Models.Bookings;
using EventApi.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Application.Bookings
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _dbContext;
        private readonly IKeyedLockManager _lockManager;
        private readonly ILogger<BookingService> _logger;

        // По замечанию ревью спринта 4 сделал _bookingLock статическим, чтобы при scoped регистрации сервиса вызовы из разных инстансов разделяли один и тот же локер
        // 19.11.2026: по замечанию из ревью спринта 4 заменил статический lock на синглтон объект IKeyedLockManager
        //private static readonly object _bookingLock = new();

        public BookingService(
            AppDbContext dbContext,
            IKeyedLockManager lockManager,
            ILogger<BookingService> logger
            )
        {
            _dbContext = dbContext;
            _lockManager = lockManager;
            _logger = logger;
        }

        public async Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
        {
            Booking booking;

            //lock (_bookingLock)
            using (await _lockManager.AcquireEventLockAsync(eventId, ct))
            {
                var evnt = await _dbContext.Events.FindAsync(eventId, ct);
                if (evnt == null)
                    throw new NotFoundException($"Событие {eventId} не найдено. Бронирование по нему не создано.");

                if (!evnt.TryReserveSeats())
                    throw new NoAvailableSeatsException("Недостаточно свободных мест для этого бронирования");

                booking = Booking.Create(eventId);
                await _dbContext.Bookings.AddAsync(booking, ct);
                await _dbContext.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Создано бронирование {0} на событие {1}.", booking.Id, booking.EventId);
            return new BookingDto(booking);
        }

        public async Task<BookingDto> ConfirmBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = await _dbContext.Bookings.FindAsync(id, ct);
            if (booking == null)
                throw new NotFoundException($"Бронирование {id} не найдено.");

            booking.Confirm();
            await _dbContext.SaveChangesAsync(ct);

            return new BookingDto(booking);
        }

        public async Task<BookingDto> RejectBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = await _dbContext.Bookings.FindAsync(id, ct);
            if (booking == null)
                throw new NotFoundException($"Бронирование {id} не найдено.");

            //lock (_bookingLock)
            using (await _lockManager.AcquireEventLockAsync(booking.EventId, ct))
            {
                booking.Reject();
                _dbContext.Bookings.Update(booking);

                var evnt = await _dbContext.Events.FindAsync(booking.EventId, ct);
                if (evnt == null)
                    throw new NotFoundException($"Событие {booking.EventId} не найдено. Отмены бронирования по нему не произошло.");

                evnt.ReleaseSeats();
                await _dbContext.SaveChangesAsync(ct);
            }

            return new BookingDto(booking);
        }

        public async Task<BookingDto> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            var booking = await _dbContext.Bookings.FindAsync(id, ct);
            if (booking == null)
                throw new NotFoundException(string.Format("Бронирование {0} не найдено.", id));

            return new BookingDto(booking);
        }

        public async Task<List<BookingDto>> GetBookingsAsync(CancellationToken ct = default)
        {
            // Список всех бронирований
            var bookings = _dbContext.Bookings
                .AsNoTracking()
                .AsQueryable();
            var dtos = await bookings.Select(b => new BookingDto(b))
                .ToListAsync();

            return dtos;
        }
    }
}
