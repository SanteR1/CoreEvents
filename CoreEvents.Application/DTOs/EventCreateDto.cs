using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Application.DTOs
{
    public record EventCreateDto(
        [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно")]
        [MaxLength(200, ErrorMessage = "Максимальная длина 200 символов")]
        string? Title,
        [Required(ErrorMessage = "Дата начала обязательна")]
        DateTime? StartAt,
        [Required(ErrorMessage = "Дата окончания обязательна")]
        DateTime? EndAt,
        [Required(ErrorMessage = "Общее количество мест обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество мест должно быть больше нуля")]
        int? TotalSeats,
        [MaxLength(2000, ErrorMessage = "Максимальная длина 2000 символов")]
        string? Description = null
    );
}
