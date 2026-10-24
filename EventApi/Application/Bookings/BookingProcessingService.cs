using EventApi.Models.Bookings;

namespace EventApi.Application.Bookings
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IBookingStore _store;
        private readonly ILogger<BookingProcessingService> _logger;
        private TimeSpan _interval = TimeSpan.FromSeconds(5);

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

            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessBookings(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "При обработке новых бронирований произошла ошибка.");
                }
            }

            _logger.LogInformation("BookingProcessingService остановлен.");
        }

        private async Task ProcessBookings(CancellationToken ct)
        {
            var pending = _store.GetAll()
                .Where(b => b.Status == BookingStatus.Pending).ToList<Booking>();

            foreach (var booking in pending)
            {
                booking.Confirm();

                _logger.LogInformation("Бронирование {0} подтверждено.", booking.Id);

                // Имитируем задержку
                await Task.Delay(2000, ct);
            }
        }
    }
}
