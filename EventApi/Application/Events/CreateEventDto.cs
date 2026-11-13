using EventApi.Models.Events;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Application.Events
{
    public class CreateEventDto : IValidatableObject
    {
        [Required(ErrorMessage = "Заголовок события обязателен.")]
        public string Title { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage = "Дата начала события обязательна.")]
        public DateTime? StartAt { get; set; }
        [Required(ErrorMessage = "Дата окончания события обязательна.")]
        public DateTime? EndAt { get; set; }
        [Required(ErrorMessage = "Общее количество мест обязательно.")]
        public int? TotalSeats { get; set; }

        public CreateEventDto() { Title = string.Empty; }
        public CreateEventDto(Event evnt)
        {
            Title = evnt.Title;
            Description = evnt.Description;
            StartAt = evnt.StartAt;
            EndAt = evnt.EndAt;
            TotalSeats = evnt.TotalSeats;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> errors = new List<ValidationResult>();

            // Проверку EndAt > StartAt реализуем здесь
            if (!(EndAt > StartAt))
            {
                errors.Add(new ValidationResult("Дата окончания должна быть позже даты начала события.", ["Дата окончания"]));
            }

            // Проверка на минимальное значение TotalSeats
            if (TotalSeats <= 0)
            {
                errors.Add(new ValidationResult("Общее количество мест должно быть больше 0.", ["Общее количество мест"]));
            }


            return errors;
        }

        public void ThrowsExceptionIfInvalid()
        {
            var context = new ValidationContext(this, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(this, context, results, validateAllProperties: true);

            if (!isValid)
            {
                var errors = results.Select(r => r.ErrorMessage);
                var summary = string.Join(Environment.NewLine, errors);
                throw new ValidationException(summary);
            }
        }
    }
}
