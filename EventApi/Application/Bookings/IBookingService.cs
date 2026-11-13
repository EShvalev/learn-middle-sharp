namespace EventApi.Application.Bookings
{
    public interface IBookingService
    {
        public Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct);
        public Task<BookingDto> GetBookingByIdAsync(Guid id, CancellationToken ct);
        public Task<BookingDto> ConfirmBookingAsync(Guid id, CancellationToken ct);
        public Task<BookingDto> RejectBookingAsync(Guid id, CancellationToken ct);
        public Task<List<BookingDto>> GetBookingsAsync(CancellationToken ct);
    }
}
