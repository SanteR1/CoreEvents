using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public record EventCreateDto(
        [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно")]
        string Title,
        string? Description,
        [Required(ErrorMessage = "Дата начала обязательна")]
        DateTime StartAt,
        [Required(ErrorMessage = "Дата окончания обязательна")]
        DateTime EndAt,
        [Required(ErrorMessage = "Общее количество мест обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество мест должно быть больше нуля")]
        int? TotalSeats
    );
}
