using EventApi.Models.Bookings;
using EventApi.Models.Events;
using System.Collections.Concurrent;

namespace EventApi.Application.Bookings
{
    public class InMemoryBookingStore : IBookingStore
    {
        private readonly ConcurrentDictionary<Guid, Booking> _bookings = new ConcurrentDictionary<Guid, Booking>();

        public async Task CreateAsync(Booking booking, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(booking);

            if (!_bookings.TryAdd(booking.Id, booking.Clone()))
            {
                throw new InvalidOperationException($"Бронирование с ID {booking.Id} уже существует.");
            }
        }

        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _bookings.TryGetValue(id, out var booking) ? booking.Clone() : null;
        }

        public async Task UpdateAsync(Guid id, Booking booking, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(booking);

            if (!_bookings.TryGetValue(id, out var _))
                throw new EventNotFoundException($"Бронирование {id} не найдено.");

            _bookings[id] = booking.Clone();
        }

        public async Task<IEnumerable<Booking>> GetAllAsync(CancellationToken ct = default) => _bookings.Values.Select(e => e.Clone());
    }
}
