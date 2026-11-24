using Microsoft.AspNetCore.Mvc;

namespace EventApi.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base("Ресурс не найден") { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }

        public ProblemDetails ToProblemDetails()
            => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ресурс не найден",
                Detail = Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            };
    }
}
