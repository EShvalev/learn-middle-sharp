namespace EventApi.Models.Bookings
{
    public class BookingCreateResponse
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public BookingStatus Status { get; set; }
    }
}
