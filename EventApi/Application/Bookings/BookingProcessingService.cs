using EventApi.Application.Events;
using EventApi.DataAсcess;
using EventApi.Models.Bookings;
using EventApi.Models.Events;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Application.Bookings
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IKeyedLockManager _lockManager;
        private readonly ILogger<BookingProcessingService> _logger;
        private readonly TimeSpan _timerPeriod = TimeSpan.FromSeconds(5);
        private readonly int _processingDelay = 2000;
        // 19.11.2026: по замечанию из ревью спринта 4 заменил _processingSemaphore на синглтон объект IKeyedLockManager
        //private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        public BookingProcessingService(
            IServiceScopeFactory scopeFactory,
            IKeyedLockManager lockManager,
            ILogger<BookingProcessingService> logger
            )
        {
            _scopeFactory = scopeFactory;
            _lockManager = lockManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingProcessingService запущен.");

            using var timer = new PeriodicTimer(_timerPeriod);

            try
            {
                do
                {
                    try
                    {
                        await ProcessBookings(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "При обработке новых бронирований произошла ошибка.");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BookingProcessingService: сработал CancellationToken, завершение цикла обработки.");
            }
            finally
            {
                _logger.LogInformation("BookingProcessingService остановлен.");
            }
        }

        private async Task ProcessBookings(CancellationToken ct)
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var bookings = context.Bookings //.GetAllAsync(ct);
                .AsNoTracking()
                .AsEnumerable();
            var pending = bookings
                .Where(booking => booking.Status == BookingStatus.Pending).ToList<Booking>();
            var tasks = pending
                .Select(booking => ProcessBookingAsync(booking, ct))
                .ToList();

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken ct)
        {
            // Имитируем задержку
            await Task.Delay(_processingDelay, ct);

            // Семафор входит в асинхронное ожидание
            //await _processingSemaphore.WaitAsync(ct);

            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Event? evnt = null;
            bool needReleaseSeats = false;

            using (await _lockManager.AcquireEventLockAsync(booking.EventId, ct))
            {
                try
                {
                    evnt = await context.Events.FindAsync(booking.EventId, ct);
                    if (evnt == null)
                    {
                        booking.Reject();
                        context.Bookings.Update(booking);
                        _logger.LogWarning("[{Timestamp}]: Событие {EventId} не найдено, бронирование {BookingId} отклонено.", DateTime.Now, booking.EventId, booking.Id);
                    }
                    else
                    {
                        booking.Confirm();
                        context.Bookings.Update(booking);
                        _logger.LogInformation("[{Timestamp}]: Бронирование {BookingId} подтверждено.", DateTime.Now, booking.Id);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("Во время бронирования сработало прерывание операции.");
                    throw; // Пробрасываем наверх, чтобы обработки выше корректно завершился
                }
                catch (Exception ex)
                {
                    needReleaseSeats = true;

                    if (booking.Status != BookingStatus.Rejected)
                    {
                        booking.Reject();
                        context.Bookings.Update(booking);
                        _logger.LogError(ex, "При обработке бронирования {BookingId} произошла ошибка, бронирование отклонено.", booking.Id);
                    }
                }
                finally
                {
                    if (needReleaseSeats && evnt != null)
                    {
                        evnt.ReleaseSeats();
                        context.Events.Update(evnt);
                    }

                    // Сохраняем все изменения
                    await context.SaveChangesAsync(ct);

                    //_processingSemaphore.Release();
                }
            }
        }
    }
}
