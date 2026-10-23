namespace EventApi.Models.Bookings
{
    public class Booking
    {
        public Guid Id { get; init; }
        public Guid EventId { get; init; }
        public BookingStatus Status { get; private set; }
        public DateTime CreatedAt { get; init; }
        public DateTime? ProcessedAt { get; set; }

        // Фабричный метод создания/инициализации
        public static Booking CreatePending(Guid eventId)
        {
            var booking = new Booking()
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };
            return booking;
        }

        public void Confirm()
        {
            if (Status != BookingStatus.Pending)
                throw new BookingStatusException(string.Format("Данный переход между статусами бронирования запрещен: текущий {0}, новый {1}.", nameof(Status), nameof(BookingStatus.Confirmed)));
            
            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.Now;
        }

        public void Reject()
        {
            if (Status != BookingStatus.Pending)
                throw new BookingStatusException(string.Format("Данный переход между статусами бронирования запрещен: текущий {0}, новый {1}.", nameof(Status), nameof(BookingStatus.Rejected)));

            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.Now;
        }
    }
}
