using System.ComponentModel.DataAnnotations;

namespace Bookings.Application.DTOs;

public record CreateBookingRequestDto(
    [Range(1, 100, ErrorMessage = "Количество мест должно быть в диапазоне от 1 до 100")]
    int? Seats = 1
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Seats is < 1 or > 100)
        {
            yield return new ValidationResult("Количество мест должно быть в диапазоне от 1 до 100", [nameof(Seats)]);
        }
    }
}
