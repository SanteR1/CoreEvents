using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public record PagedFilter(
        [property: Range(1, int.MaxValue, ErrorMessage = "Значение должно быть больше 0")]
        int Page = 1,
        [property: Range(1, int.MaxValue, ErrorMessage = "Значение должно быть больше 0")]
        int PageSize = 10
        );
}
