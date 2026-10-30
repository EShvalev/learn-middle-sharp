namespace EventApi.Models.Bookings
{
    public class BookingDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public BookingDto(Booking booking)
        {
            Id = booking.Id;
            EventId = booking.EventId;
            Status = booking.Status;
            CreatedAt = booking.CreatedAt;
            ProcessedAt = booking.ProcessedAt;
        }
    }
}
