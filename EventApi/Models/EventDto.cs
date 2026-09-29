using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.OpenApi;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Models
{
    public class EventDto : IValidatableObject
    {
        [Required(ErrorMessage = "Заголовок события обязателен.")]
        public string Title { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage = "Дата начала события обязательна.")]
        public DateTime? StartAt { get; set; }
        [Required(ErrorMessage = "Дата окончания события обязательна.")]
        public DateTime? EndAt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> errors = new List<ValidationResult>();

            // Проверку EndAt > StartAt реализуем здесь
            if (!(EndAt > StartAt))
            {
                errors.Add(new ValidationResult("Дата окончания должна быть позже даты начала события.", new string[] { "Дата окончания" }));
            }

            return errors;
        }
    }
}
