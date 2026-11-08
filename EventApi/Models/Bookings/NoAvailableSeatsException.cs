namespace EventApi.Models.Bookings
{
    public class NoAvailableSeatsException : Exception
    {
        public NoAvailableSeatsException() : base("Недостаточно свободных мест.") { }
        public NoAvailableSeatsException(string message) : base(message) { }
        public NoAvailableSeatsException(string message, Exception innerException) : base(message, innerException) { }
    }
}
