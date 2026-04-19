using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public record EventFilter(
        string? Title = null,
        DateTime? From = null,
        DateTime? To = null,
        [Range(1, 100000, ErrorMessage = "Значение должно быть больше 0")]
        int Page = 1,
        [Range(1, 100, ErrorMessage = "Значение должно быть больше 0")]
        int PageSize = 10
    ):PagedFilter(Page,PageSize);
}
