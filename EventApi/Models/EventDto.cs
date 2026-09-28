using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EventApi.Models
{
    public class EventDto
    {
        [Required(AllowEmptyStrings = true, ErrorMessage = "Заголовок события обязателен.")]
        public string Title { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage = "Дата начала события обязательна.")]
        public DateTime StartAt { get; set; }
        [Required(ErrorMessage = "Дата окончания события обязательна.")]
        public DateTime EndAt { get; set; }
    }
}
