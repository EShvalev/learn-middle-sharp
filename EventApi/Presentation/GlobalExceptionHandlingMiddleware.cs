using EventApi.Application.Exceptions;
using EventApi.Models.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace EventApi.Presentation
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleException(httpContext, ex);
            }
        }

        private async Task HandleException(HttpContext httpContext, Exception ex)
        {
            var problemDetails = MapStatusCode(ex);

            // Логируем только 500 ошибки (учел замечание ревью по спринту 4)
            if (problemDetails.Status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    httpContext.Request.Headers["x-request-id"]);
            }

            if (httpContext.Response.HasStarted)
            {
                return;
            }

            var contentType = "application/problem+json";
            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = contentType;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType(), System.Text.Json.JsonSerializerOptions.Default, contentType);
        }

        private static ProblemDetails MapStatusCode(Exception ex)
            => ex switch
            {
                BookingStatusException tex => tex.ToProblemDetails(),
                NotFoundException tex => tex.ToProblemDetails(),
                ValidationException tex => tex.ToProblemDetails(),
                NoAvailableSeatsException tex => tex.ToProblemDetails(),
                _ => new ProblemDetails()
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Внутренняя ошибка сервера",
                    Detail = ex.Message,
                    Type = "https://www.rfc-editor.org/info/rfc9110/#section-15.5.10"
                }
            };
    }
}
