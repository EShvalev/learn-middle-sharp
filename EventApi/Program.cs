using EventApi.Application;
using EventApi.Application.Bookings;
using EventApi.Application.Events;
using EventApi.DataAсcess;
using EventApi.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
                // Оставляем автоматическую проверку валидации (options.SuppressModelStateInvalidFilter = true), настраиваем только единый формат ошибки + логирование.
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

                    logger.LogError("Ошибка валидации: {errors}", errorsString);

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
        builder.Services.AddSingleton<IKeyedLockManager, KeyedLockManager>();
        builder.Services.AddSingleton<IEventRepository, EventRepository>();
        builder.Services.AddSingleton<IBookingRepository, BookingRepository>();
        builder.Services.AddScoped<IEventService, EventService>();
        builder.Services.AddScoped<IBookingService, BookingService>();

        builder.Services.AddHostedService<BookingProcessingService>();

        // Регистрируем AppDbContext
        // Замечание: используем snake_case naming convention для генерации имен в БД PostgreSQL (это стандарт для данной СУБД).
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                //.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name })
                ;

                if (builder.Environment.IsDevelopment())
                {
                    options
                        .EnableDetailedErrors()
                        .EnableSensitiveDataLogging();
                }
            });

        var app = builder.Build();

        // Регистрируем глобальный обработчик ошибок
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Гарантируем создание объектов БД, если еще не созданы
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical(ex, "Не удалось подключиться к БД при запуске приложения. Проверьте, запущен ли docker контейнер БД.");
            throw;
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}