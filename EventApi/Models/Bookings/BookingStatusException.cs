namespace EventApi.Models.Bookings
{
    public class BookingStatusException : Exception
    {
        public BookingStatusException() : base("Недопустимый статус бронирования.") { }
        public BookingStatusException(string message) : base(message) { }
        public BookingStatusException(string message, Exception innerException) : base(message, innerException) { }
    }
}
