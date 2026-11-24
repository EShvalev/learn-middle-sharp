using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EventApi.Application.Exceptions
{
    public sealed class ValidationException : Exception
    {
        public IDictionary<string, ICollection<string>> Errors { get; } = new Dictionary<string, ICollection<string>>();

        public ValidationException(IDictionary<string, ICollection<string>> errors) : base("Ошибка валидации")
        {
            Errors = errors;
        }

        public ValidationException(string field, string error) : base("Ошибка валидации")
        {
            Errors = new Dictionary<string, ICollection<string>>
            {
                { field, new[] { error } }
            };
        }

        public ProblemDetails ToProblemDetails()
            => new ProblemDetails()
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации",
                Detail = string.Join(Environment.NewLine, Errors.Select(kv => $"{kv.Key}: {string.Join(";", kv.Value!.Select(s => s))}")),
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            };
    }
}
