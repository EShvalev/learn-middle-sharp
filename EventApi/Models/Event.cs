using System.ComponentModel.DataAnnotations;

namespace EventApi.Models
{
    public class Event : IValidatableObject
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> errors = new List<ValidationResult>();

            // Проверку EndAt > StartAt реализуем здесь
            if (!(EndAt > StartAt))
            {
                errors.Add(new ValidationResult("Дата окончания должна быть позже даты начала события.", new string[] { "Дата окончания" }));

                // Для задания 2 спринта выбрасываем исключение
                throw new ValidationException("Дата окончания должна быть позже даты начала события.");
            }

            return errors;
        }
    }
}
