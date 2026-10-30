using EventApi.Models.Bookings;

namespace EventApi.Application.Bookings
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IBookingStore _store;
        private readonly ILogger<BookingProcessingService> _logger;
        private readonly TimeSpan _timerPeriod = TimeSpan.FromSeconds(5);
        const int _taskDelay = 2000;

        public BookingProcessingService(
            IBookingStore store,
            ILogger<BookingProcessingService> logger
            )
        {
            _store = store;
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
            var pending = _store.GetAll()
                .Where(booking => booking.Status == BookingStatus.Pending).ToList<Booking>();
            var tasks = pending
                .Select(booking => ProcessBookingAsync(booking, ct)).ToList();

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken ct)
        {
            // Имитируем задержку
            await Task.Delay(_taskDelay, ct);

            booking.Confirm();
            _logger.LogInformation("Бронирование {0} подтверждено.", booking.Id);
        }
    }
}
