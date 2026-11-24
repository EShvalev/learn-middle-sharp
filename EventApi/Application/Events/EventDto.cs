using EventApi.Models.Events;
using System.Diagnostics.CodeAnalysis;

namespace EventApi.Application.Events
{
    public sealed record EventDto
    {
        public required Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public required DateTime StartAt { get; set; }
        public required DateTime EndAt { get; set; }
        public required int TotalSeats { get; set; }
        public required int AvailableSeats { get; set; }

        [SetsRequiredMembers]
        public EventDto(Event evnt)
        {
            Id = evnt.Id;
            Title = evnt.Title;
            Description = evnt.Description;
            StartAt = evnt.StartAt;
            EndAt = evnt.EndAt;
            TotalSeats = evnt.TotalSeats;
            AvailableSeats = evnt.AvailableSeats;
        }
    }
}
