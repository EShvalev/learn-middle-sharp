
using EventApi.Application;
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

                    var customResponse = new
                    {
                        Message = "Ошибки валидации",
                        Errors = errors
                    };

                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                    var errorsString = string.Join(Environment.NewLine, errors.Select(kv => $"{kv.Key}: {kv.Value}"));

                    logger.LogError($"Ошибка валидации: {errorsString}");

                    return new BadRequestObjectResult(customResponse);
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddSwaggerGen();

        // Регистрируем EventService
        builder.Services.AddScoped<IEventService, EventService>();

        var app = builder.Build();

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
