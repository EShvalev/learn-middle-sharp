using Microsoft.AspNetCore.Mvc;

namespace EventApi.Models.Bookings
{
    public class BookingStatusException : Exception
    {
        public BookingStatusException() : base("Недопустимый статус бронирования.") { }
        public BookingStatusException(string message) : base(message) { }
        public BookingStatusException(string message, Exception innerException) : base(message, innerException) { }

        public ProblemDetails ToProblemDetails()
            => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Недопустимый статус",
                Detail = Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            };
    }
}
