using EventApi.Models.Bookings;

namespace EventApi.Application.Bookings
{
    public interface IBookingStore
    {
        public void Add(Booking booking);
        public bool Get(Guid id, out Booking booking);
        public IEnumerable<Booking> GetAll();
    }
}
