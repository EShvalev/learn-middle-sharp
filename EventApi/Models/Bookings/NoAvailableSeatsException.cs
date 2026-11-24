using Microsoft.AspNetCore.Mvc;

namespace EventApi.Models.Bookings
{
    public class NoAvailableSeatsException : Exception
    {
        public NoAvailableSeatsException() : base("Недостаточно свободных мест") { }
        public NoAvailableSeatsException(string message) : base(message) { }
        public NoAvailableSeatsException(string message, Exception innerException) : base(message, innerException) { }

        public ProblemDetails ToProblemDetails()
            => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Недостаточно свободных мест",
                Detail = Message,
                Type = "https://www.rfc-editor.org/info/rfc9110/#section-15.5.10"
            };
    }
}
