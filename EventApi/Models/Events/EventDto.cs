using System.ComponentModel.DataAnnotations;

namespace EventApi.Models.Events
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        
        public EventDto() { Title = string.Empty; }

        public EventDto(Event evnt)
        {
            Id = evnt.Id;
            Title = evnt.Title;
            Description = evnt.Description;
            StartAt = evnt.StartAt;
            EndAt = evnt.EndAt;
        }
    }
}
