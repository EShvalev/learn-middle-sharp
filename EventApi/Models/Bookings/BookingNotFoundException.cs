namespace EventApi.Models.Bookings
{
    public class BookingNotFoundException : Exception
    {
        public BookingNotFoundException() : base("Бронирование не найдено.") { }
        public BookingNotFoundException(string message) : base(message) { }
        public BookingNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
