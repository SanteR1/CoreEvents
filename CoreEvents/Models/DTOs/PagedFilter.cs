using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public record PagedFilter(
        [Range(1, 100000, ErrorMessage = "Значение должно быть больше 0")]
        int Page = 1,
        [Range(1, 100, ErrorMessage = "Значение должно быть больше 0")]
        int PageSize = 10
        );
}
