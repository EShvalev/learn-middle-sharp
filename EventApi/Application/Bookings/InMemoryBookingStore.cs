using EventApi.Models.Bookings;
using EventApi.Models.Events;
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

        public bool Update(Guid id, Booking booking)
        {
            return Get(id, out var orig_evnt) ? _dict.TryUpdate(id, booking, orig_evnt) : false;
        }

        public IEnumerable<Booking> GetAll() => _dict.Values;
    }
}
