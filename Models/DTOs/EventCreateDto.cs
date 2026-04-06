using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public record EventCreateDto(
        [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Название должно быть от 2 до 100 символов")]
        string Title,
        string? Description,
        [Required(ErrorMessage = "Дата начала обязательна")]
        [Range(typeof(DateTime), "2020-01-01", "2030-12-31", ErrorMessage = "Дата начала вне диапазона")]
        DateTime StartAt,
        [Required(ErrorMessage = "Дата окончания обязательна")]
        [Range(typeof(DateTime), "2020-01-01", "2030-12-31", ErrorMessage = "Дата окончания вне диапазона")]
        DateTime EndAt
    );
}
