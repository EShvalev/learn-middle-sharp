using EventApi.Models.Bookings;
using System.Collections.Concurrent;

namespace EventApi.Application.Bookings
{
    public class InMemoryBookingStore : IBookingStore
    {
        private readonly ConcurrentDictionary<Guid, Booking> _dict = new ConcurrentDictionary<Guid, Booking>();

        public void Add(Booking booking)
        {
            _dict[booking.Id] = booking;
        }

        public bool Get(Guid id, out Booking booking)
        {
            return _dict.TryGetValue(id, out booking);
        }

        public IEnumerable<Booking> GetAll() => _dict.Values;
    }
}
