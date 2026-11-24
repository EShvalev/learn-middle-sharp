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
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _processingDelay = TimeSpan.FromSeconds(2);
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

            using var timer = new PeriodicTimer(_pollingInterval);

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

            var pendingIds = await context.Bookings
                .Where(booking => booking.Status == BookingStatus.Pending)
                .Select(booking => booking.Id)
                .ToListAsync();

            var tasks = pendingIds
                .Select(bookingId => ProcessBookingAsync(bookingId, ct))
                .ToList();

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookingAsync(Guid bookingId, CancellationToken ct)
        {
            // Имитируем задержку
            await Task.Delay(_processingDelay, ct);

            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await context.Bookings.FindAsync(bookingId, ct);
            if (booking == null)
                return;

            Event? evnt = await context.Events.FindAsync(booking.EventId, ct);

            //await _processingSemaphore.WaitAsync(ct);
            using (await _lockManager.AcquireEventLockAsync(booking.EventId, ct))
            {
                try
                {
                    if (evnt == null)
                    {
                        booking.Reject();
                        _logger.LogWarning("[{Timestamp}]: Событие {EventId} не найдено, бронирование {BookingId} отклонено.", DateTime.Now, booking.EventId, booking.Id);
                    }
                    else
                    {
                        booking.Confirm();
                        _logger.LogInformation("[{Timestamp}]: Бронирование {BookingId} подтверждено.", DateTime.Now, booking.Id);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("При обработке бронирования {BookingId} сработало прерывание операции.", booking.Id);
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (booking.Status != BookingStatus.Rejected)
                        {
                            booking.Reject();

                            if (evnt != null)
                                evnt.ReleaseSeats();

                            _logger.LogError(ex, "При обработке бронирования {BookingId} произошла ошибка, бронирование отклонено.", booking.Id);
                        }
                    }
                    catch (Exception relEx)
                    {
                        _logger.LogError(relEx, "При обработке бронирования {BookingId} произошла ошибка, попытка отклонить бронирование и вернуть места не удалась.", booking.Id);
                    }
                }
                finally
                {
                    // Сохраняем все изменения
                    await context.SaveChangesAsync(ct);
                }
            }
        }
    }
}
