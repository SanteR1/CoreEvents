using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public record EventUpdateDto(
        [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно")]
        string? Title,
        [Required(ErrorMessage = "Дата начала обязательна")]
        DateTime? StartAt,
        [Required(ErrorMessage = "Дата окончания обязательна")]
        DateTime? EndAt,
        string? Description = null);
}
