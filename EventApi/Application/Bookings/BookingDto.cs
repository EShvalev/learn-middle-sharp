using EventApi.Models.Bookings;
using System.Diagnostics.CodeAnalysis;

namespace EventApi.Application.Bookings
{
    public sealed record BookingDto
    {
        public required Guid Id { get; set; }
        public required Guid EventId { get; set; }
        public required BookingStatus Status { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        [SetsRequiredMembers]
        public BookingDto(Booking booking)
        {
            Id = booking.Id;
            EventId = booking.EventId;
            Status = booking.Status;
            CreatedAt = booking.CreatedAt;
            ProcessedAt = booking.ProcessedAt;
        }
    }
}
