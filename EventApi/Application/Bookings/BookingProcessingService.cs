using EventApi.Application.Events;
using EventApi.Models.Events;
using EventApi.Models.Bookings;

namespace EventApi.Application.Bookings
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IEventStore _eventStore;
        private readonly IBookingStore _bookingStore;
        private readonly ILogger<BookingProcessingService> _logger;
        private readonly TimeSpan _timerPeriod = TimeSpan.FromSeconds(5);
        private readonly int _processingDelay = 2000;
        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        public BookingProcessingService(
            IEventStore eventStore,
            IBookingStore bookingStore,
            ILogger<BookingProcessingService> logger
            )
        {
            _eventStore = eventStore;
            _bookingStore = bookingStore;
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
            var pending = _bookingStore.GetAll()
                .Where(booking => booking.Status == BookingStatus.Pending).ToList<Booking>();
            var tasks = pending
                .Select(booking => ProcessBookingAsync(booking, ct)).ToList();

            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken ct)
        {
            // Имитируем задержку
            await Task.Delay(_processingDelay, ct);

            // Семафор входит в асинхронное ожидание
            await _processingSemaphore.WaitAsync(ct);

            Event? evnt = null;
            try
            {
                if (!_eventStore.Get(booking.EventId, out evnt))
                {
                    booking.Reject();
                    _bookingStore.Update(booking.Id, booking);
                    _logger.LogWarning("[{Timestamp}]: Событие {EventId} не найдено, бронирование {BookingId} отклонено.", DateTime.Now, booking.EventId, booking.Id);
                }
                else
                {
                    booking.Confirm();
                    _bookingStore.Update(booking.Id, booking);
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
                booking.Reject();
                _bookingStore.Update(booking.Id, booking);
                _logger.LogError(ex, "При обработке бронирования {BookingId} произошла ошибка, бронирование отклонено. ", booking.Id);

                if (evnt != null)
                {
                    evnt.ReleaseSeats();
                    _eventStore.Update(evnt.Id, evnt);
                    _logger.LogError(ex, "Место в событии {EventId} возвращено в доступные.", booking.EventId);
                }
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
    }
}
