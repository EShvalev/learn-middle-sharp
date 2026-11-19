using EventApi.Models.Bookings;
using EventApi.Models.Events;

namespace EventApi.Application.Bookings
{
    public interface IBookingStore
    {
        public Task CreateAsync(Booking booking, CancellationToken ct);
        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct);
        public Task UpdateAsync(Guid id, Booking booking, CancellationToken ct);
        public Task<IEnumerable<Booking>> GetAllAsync(CancellationToken ct);
    }
}
