using EventApi.Application.Events;
using EventApi.Application.Bookings;
using EventApi.Presentation;
using Microsoft.AspNetCore.Mvc;

namespace EventApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // Оставляем автоматическую проверку валидации, настраиваем только единый формат ошибки + логирование.
                //options.SuppressModelStateInvalidFilter = true;

                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(kv => kv.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kv => kv.Key,
                            kv => string.Join(";", kv.Value!.Errors.Select(e => e.ErrorMessage)));

                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                    var errorsString = string.Join(Environment.NewLine, errors.Select(kv => $"{kv.Key}: {kv.Value}"));

                    logger.LogError($"Ошибка валидации: {errorsString}");

                    var customResponse = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Detail = errorsString
                    };

                    return new BadRequestObjectResult(customResponse);
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddSwaggerGen();

        // Регистрируем свои сервисы
        builder.Services.AddSingleton<IBookingStore, InMemoryBookingStore>();
        builder.Services.AddScoped<IEventService, EventService>();
        builder.Services.AddScoped<IBookingService, BookingService>();
        
        builder.Services.AddHostedService<BookingProcessingService>();

        var app = builder.Build();

        // Регистрируем глобальный обработчик ошибок
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
