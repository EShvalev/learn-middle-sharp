using EventApi.Models.Bookings;

namespace EventApi.Application.Bookings
{
    public interface IBookingService
    {
        public Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct);
        public Task<Booking> GetBookingByIdAsync(Guid id, CancellationToken ct);
    }
}
